using UnityEngine;

[CreateAssetMenu(fileName = "GameManagerData", menuName = "Dojo/GameManagerData", order = 0)]
public class GameManagerData : ScriptableObject
{
    [Header("RPC Configuration")]
    [Tooltip("URL for the Starknet RPC endpoint (Katana) (e.g., http://localhost:5050)")]
    public string rpcUrl = "http://localhost:5050";

    [Header("Master Account Configuration")]
    [Tooltip("Address of the pre-funded master account (e.g., Katana's default burner admin)")]
    public string masterAccountAddress = "";

    [Tooltip("Private key for the master account")]
    public string masterAccountPrivateKey = "";

    [Header("Contract Addresses")]
    [Tooltip("Address of your deployed 'actions' contract/system")]
    public string actionsContractAddress = "";
}
