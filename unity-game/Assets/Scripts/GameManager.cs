using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using dojo_bindings;

public class GameManager : MonoBehaviour {
  [SerializeField] private List<Mole> moles;

  // Blockchain integration fields
  [Header("Dojo Blockchain")]
  [SerializeField] private WorldManager worldManager;
  [SerializeField] private GameManagerData gameManagerData;
  [SerializeField] private Actions actionsSystem;

  private JsonRpcClient provider;
  private Account masterAccount;
  private BurnerManager burnerManager;
  private Account currentAccount;

  private float blockchainUpdateInterval = 1f;
  private float blockchainTimer = 0f;

  [Header("UI objects")]
  [SerializeField] private GameObject playButton;
  [SerializeField] private GameObject gameUI;
  [SerializeField] private GameObject outOfTimeText;
  [SerializeField] private GameObject bombText;
  [SerializeField] private TMPro.TextMeshProUGUI timeText;
  [SerializeField] private TMPro.TextMeshProUGUI scoreText;

  // Hardcoded variables you may want to tune.
  private float startingTime = 30f;

  // Global variables
  private float timeRemaining;
  private HashSet<Mole> currentMoles = new HashSet<Mole>();
  private int score;
  private bool playing = false;

  // This is public so the play button can see it.
  public async void StartGame() {
    // Deploy a new burner account for this game session
    // You should disable this on a actual game
    try {
      Debug.Log("Creating new burner account for this game session...");
      currentAccount = await burnerManager.DeployBurner();
      Debug.Log($"Created new burner account: {currentAccount.Address.Hex()}");
    } catch (System.Exception e) {
      Debug.LogError($"Failed to create burner account: {e.Message}");
      // Fall back to master account if burner creation fails
      currentAccount = masterAccount;
    }

    // Hide/show the UI elements we don't/do want to see.
    playButton.SetActive(false);
    outOfTimeText.SetActive(false);
    bombText.SetActive(false);
    gameUI.SetActive(true);
    // Hide all the visible moles.
    for (int i = 0; i < moles.Count; i++) {
      moles[i].Hide();
      moles[i].SetIndex(i);
    }
    // Remove any old game state.
    currentMoles.Clear();
    // Start with 30 seconds.
    timeRemaining = startingTime;
    score = 0;
    scoreText.text = "0";
    playing = true;

    // Notify blockchain that game has started
    if (actionsSystem != null && currentAccount != null)
    {
      try { await actionsSystem.start_game(currentAccount); }
      catch (System.Exception e) { Debug.LogError($"Blockchain start_game failed: {e.Message}"); }
    }
  }

  public async void GameOver(int type) {
    // Show the message.
    if (type == 0) {
      outOfTimeText.SetActive(true);
      Debug.Log("Game over: Out of time");
    } else {
      bombText.SetActive(true);
      Debug.Log("Game over: Hit a bomb");
    }
    // Hide all moles.
    foreach (Mole mole in moles) {
      mole.StopGame();
    }
    // Stop the game and show the start UI.
    playing = false;
    playButton.SetActive(true);

    // Blockchain game_over call with proper error handling
    if (actionsSystem != null && currentAccount != null)
    {
      try {
        byte reasonByte = (byte)type;
        Debug.Log($"Sending game_over for account {currentAccount.Address.Hex()} with score {score}");
        
        // Add a small delay to allow other pending transactions to complete
        await Task.Delay(500);
        
        await actionsSystem.game_over(currentAccount, (uint)score, reasonByte);
        Debug.Log("Game session closed successfully on blockchain");
      }
      catch (System.Exception e) {
        Debug.LogError($"Blockchain game_over failed: {e.Message}");
      }
    }
  }

  void Start()
  {
    // Auto-populate moles list if not set from the Inspector
    if (moles == null || moles.Count == 0)
    {
      moles = new List<Mole>(FindObjectsByType<Mole>(FindObjectsSortMode.None));
      // Sort list to have deterministic order (optional)
      moles.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
    }
    // Auto-assign references if they were not set in Inspector
    if (worldManager == null) worldManager = GetComponent<WorldManager>();
    if (actionsSystem == null) actionsSystem = GetComponent<Actions>();

    // Initialize blockchain provider and accounts
    provider = new JsonRpcClient(gameManagerData.rpcUrl);
    masterAccount = new Account(provider, new SigningKey(gameManagerData.masterAccountPrivateKey), new FieldElement(gameManagerData.masterAccountAddress));
    burnerManager = new BurnerManager(provider, masterAccount);
    currentAccount = masterAccount;

    // Assign contract address to generated actions binding
    if (actionsSystem != null)
      actionsSystem.contractAddress = gameManagerData.actionsContractAddress;
      
    // Subscribe to event messages from SynchronizationMaster
    if (worldManager != null && worldManager.synchronizationMaster != null)
    {
        worldManager.synchronizationMaster.OnEventMessage.AddListener(HandleEventMessage);
    }
    
  }

  // Update is called once per frame
  void Update() {
    if (playing) {
      // Update time.
      timeRemaining -= Time.deltaTime;
      if (timeRemaining <= 0) {
        timeRemaining = 0;
        GameOver(0);
      }
      timeText.text = $"{(int)timeRemaining / 60}:{(int)timeRemaining % 60:D2}";
      // Throttled blockchain frame update
      blockchainTimer += Time.deltaTime;
      if (blockchainTimer >= blockchainUpdateInterval)
      {
        blockchainTimer = 0f;
        if (actionsSystem != null && currentAccount != null)
        {
          _ = actionsSystem.update_frame(currentAccount, (uint)(timeRemaining * 1000));
        }
      }

      // Check if we need to start any more moles.
      if (currentMoles.Count <= (score / 10)) {
        // Choose a random mole.
        int index = UnityEngine.Random.Range(0, moles.Count);
        // Doesn't matter if it's already doing something, we'll just try again next frame.
        if (!currentMoles.Contains(moles[index])) {
          currentMoles.Add(moles[index]);
          moles[index].Activate(score / 10);
        }
      }
    }
  }

  public void AddScore(int moleIndex) {
    if (moleIndex < 0 || moleIndex >= moles.Count || !playing) return;
    // Add and update score.
    score += 1;
    scoreText.text = $"{score}";
    // Increase time by a little bit.

    // Blockchain hit_mole call
    if (actionsSystem != null && currentAccount != null)
    {
      _ = actionsSystem.hit_mole(currentAccount, 1u);
    }
    timeRemaining += 1;
    // Remove from active moles.
    currentMoles.Remove(moles[moleIndex]);
  }

  public void Missed(int moleIndex, bool isMole) {
    if (moleIndex < 0 || moleIndex >= moles.Count) return;
    if (isMole) {
      // Decrease time by a little bit.
      timeRemaining -= 2;
    }

    // Blockchain miss_mole call
    if (actionsSystem != null && currentAccount != null)
    {
      byte isMoleByte = (byte)(isMole ? 1 : 0);
      _ = actionsSystem.miss_mole(currentAccount, isMoleByte);
    }

    // Remove from active moles.
    currentMoles.Remove(moles[moleIndex]);
  }
  
  // Handle event messages from Dojo
  private void HandleEventMessage(ModelInstance model)
  {
    // Check if this is a MoleHit event
    if (model is dojo_starter_MoleHit moleHit)
    {
      Debug.Log($"MoleHit event received: Player={moleHit.player.Hex()}, Points={moleHit.points}, Score={moleHit.score}");
      
      // Check if this event is for the current player
      if (currentAccount != null && moleHit.player.Hex() == currentAccount.Address.Hex())
      {
        // Update game state based on the MoleHit event
        // This might override the local score with the blockchain-validated score
        if (playing)
        {
          int newScore = (int)moleHit.score;
          // Only update if the score from blockchain is different
          if (score != newScore)
          {
            score = newScore;
            scoreText.text = $"{score}";
            Debug.Log($"Score updated from blockchain: {score}");
          }
          
          // Add time as a reward based on points
          timeRemaining += (float)moleHit.points;
          
          // Show floating text effect for blockchain verification
          GameObject floatingTextObj = CreateFloatingText(scoreText.transform.parent, 
              $"+{moleHit.points} VERIFIED!", 
              Color.green, 
              scoreText.transform.position + new Vector3(0, 50, 0));
          
          // Animate and destroy the floating text
          StartCoroutine(AnimateFloatingText(floatingTextObj));
        }
      }
    }
  }
  
  // Create a floating text object entirely through code
  private GameObject CreateFloatingText(Transform parent, string text, Color color, Vector3 position)
  {
    // Create a new GameObject with the necessary components
    GameObject textObj = new GameObject("FloatingText");
    textObj.transform.SetParent(parent, false);
    textObj.transform.position = position;
    
    // Add a TextMeshProUGUI component
    TMPro.TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
    tmpText.text = text;
    tmpText.color = color;
    tmpText.fontSize = 36;
    tmpText.alignment = TMPro.TextAlignmentOptions.Center;
    tmpText.fontStyle = TMPro.FontStyles.Bold;
    
    // Set up a RectTransform to position correctly in canvas
    RectTransform rectTransform = textObj.GetComponent<RectTransform>();
    rectTransform.sizeDelta = new Vector2(200, 50);
    
    return textObj;
  }
  
  // Animate the floating text that appears when blockchain events are received
  private IEnumerator AnimateFloatingText(GameObject textObj)
  {
    float duration = 2.0f;
    float elapsed = 0;
    Vector3 startPos = textObj.transform.position;
    Vector3 endPos = startPos + new Vector3(0, 100, 0);
    
    TMPro.TextMeshProUGUI tmpText = textObj.GetComponent<TMPro.TextMeshProUGUI>();
    Color originalColor = tmpText.color;
    
    while (elapsed < duration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / duration;
      
      // Move upward
      textObj.transform.position = Vector3.Lerp(startPos, endPos, t);
      
      // Fade out gradually
      Color fadingColor = originalColor;
      fadingColor.a = 1.0f - t;
      tmpText.color = fadingColor;
      
      yield return null;
    }
    
    Destroy(textObj);
  }
}
