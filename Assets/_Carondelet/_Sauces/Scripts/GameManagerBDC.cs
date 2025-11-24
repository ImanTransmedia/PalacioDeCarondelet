using UnityEngine;
using UnityEngine.Events;

public class GameManagerBDC : MonoBehaviour
{

    public static GameManagerBDC Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool isBDCMode = false;

    public bool hasStartGame = false;

    public int Score { get; set; }

    public bool hasEndGame = false;

    public static UnityAction OnGameStart;
    public static UnityAction OnGameEnd;

    public bool hasLantern = false;
    public float LanternCharge { get; set; }
    public bool isUsingLantern = false;


    private void Start()
    {
        CollectableItem.OnPickedUp += UseLantern;
        LanternController.OnLanternOver += LanternDown;
    }


    public void UseLantern()
    {
        hasLantern = true;
        AudioManagerBDC.I.PlaySFX("LanternUp");
        LanternCharge = 100;
    }

    public void LanternDown()
    {
        hasLantern = false;
        AudioManagerBDC.I.PlaySFX("LanternDown");
        LanternCharge = 0;
    }


    public void TogleBDCMode()
    {
        isBDCMode = !isBDCMode;
    }


    public void AddScore(int score)
    {
        Score += score;
    }

}
