using System.Collections;
using UnityEngine;
//using System.IO;
using UnityEngine.UI;


/// <summary>
/// The Spinner class handles all the functionality of the spinner.
/// It calculates the odds, animates the wheel spinning, writes the reward to
/// a text file, animates the reward, and resets the process.
/// </summary>
public class Spinner : MonoBehaviour
{
    [Header("Set in Inspector")]
    public int plays; //Used for testing large amounts of plays
    public GameObject button;
    public GameObject wheel;
    public GameObject starburst;
    public GameObject[] hideables;
    [Header("Set range of added wheel revolutions")]
    public int spinMin = 3;
    public int spinMax = 5;


    private GameObject[] prizes;
    private RewardOdds[] rewardOdds;
    private GameObject starInstance;
    private GameObject prize;
    private int totalOdds;
    private int randomNumber;
    private int rotationAmount;
    private int maxPlays;
    private float rotMultiplier;
    private string winningAward;
    //private string writerPath = "Assets/TextFile/SpinResults.txt";
    //private StreamWriter writer;
    private Text buttonText;
    private Vector3 startingPos;
    private bool claiming = false;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        prizes = new GameObject[wheel.transform.childCount];
        rewardOdds = new RewardOdds[prizes.Length];
        // Stores prizes in array based on their order in Hierarchy
        // Requires prizes to be placed in order that matches their placement on wheel
        for (int i = 0; i < prizes.Length; i++)
        {
            int h = i - 1;
            prizes[i] = wheel.transform.GetChild(i).gameObject;
            rewardOdds[i] = prizes[i].GetComponent<RewardOdds>();
            totalOdds += rewardOdds[i].winRate;
            // if we are not on the first item in the array, add the previous item's win rate
            // to the current item in the array. 
            if (i > 0)
            {
                rewardOdds[i].winRate += rewardOdds[h].winRate;
            }
        }
        rotMultiplier = 360 / prizes.Length; // Calculates the size of the prizes on the wheel
        buttonText = button.GetComponentInChildren<Text>();
    }

    
    // Triggered through On Click event on the play button
    public void PressButton()
    {
        if (!claiming)
        {
            CalculateOdds();
            maxPlays = plays;
        } else
        {
            NewSpin();
        }
    }

    // Randomizes the prize based on winRate set in RewardOdds script on each prize
    // Assigns asset name to string which will be sent to text file
    private void CalculateOdds()
    {
        randomNumber = Random.Range(0, totalOdds);
        button.SetActive(false);

        for (int i = 0; i < prizes.Length; i++)
        {
            int j = i + 1;
            int h = i - 1;
            
            // if this is the first item in array and our random number is in it's range
            if (i == 0 && randomNumber <= rewardOdds[i].winRate)
            {
                winningAward = prizes[i].name;
                prize = prizes[i];
                rotationAmount = Mathf.RoundToInt(((i * rotMultiplier) + (j * rotMultiplier)) / 2);
            } else if (randomNumber <= rewardOdds[i].winRate && randomNumber > rewardOdds[h].winRate)
            {
                winningAward = prizes[i].name;
                prize = prizes[i];
                if (i < prizes.Length - 1)
                {
                    rotationAmount = Mathf.RoundToInt(((i * rotMultiplier) + (j * rotMultiplier)) / 2);
                } else // if this is the last item in the array
                {
                    rotationAmount = Mathf.RoundToInt(((i * rotMultiplier) + 360) / 2);
                }
            }
            
        }
       
        StartCoroutine(Spin());
    }

    // Animates the wheel spinning
    private IEnumerator Spin()
    {
        int currentAngle = Mathf.RoundToInt(wheel.transform.eulerAngles.z);
        int randomRevs = Random.Range(spinMin, spinMax + 1) * 360;
        int spinAmount = rotationAmount + randomRevs - currentAngle;
        float speed = 1;
        float i = 0;

        while (i < spinAmount)
        {
            wheel.transform.Rotate(0, 0, speed);
            i += speed;

            if (i < 720)
            {
                speed += .3f;
            }

            if (spinAmount - i < 720 && speed > 1)
            {
                speed-= .3f;
            }
            yield return new WaitForSeconds(0.001f * Time.deltaTime * 2);
        }

        //WriteTxt();

        // If you set a large number of plays this skips the reward animation
        // and starts recalculating the odds
        if (plays > 1)
        {
            plays--;
            CalculateOdds();
        }
        else
        {
            claiming = true;
            StartCoroutine(CollectReward());
        }
    }

    // Writes the name of the object to a text file
    /*private void WriteTxt()
    {
        writer = new StreamWriter(writerPath, true);
        writer.WriteLine(maxPlays - plays + 1 + " " + winningAward);
        writer.Close();
    }*/

    // Animates reward appearing in center with starburst behind it
    private IEnumerator CollectReward()
    {

        float speed = 2;
        float t;

        // Takes prize out of wheel so it does not get disabled
        prize.transform.SetParent(prize.transform.parent.parent);
        startingPos = prize.transform.localPosition;
       
        foreach(GameObject hidable in hideables)
        {
            hidable.SetActive(false);
        }
        while (prize.transform.localPosition.y > 0)
        {
            prize.transform.Translate(0, -speed, 0);
            if (prize.transform.localPosition.y > startingPos.y * 2 / 3)
            {
                speed += .3f;

                t = (startingPos.y - prize.transform.localPosition.y) / (startingPos.y - (startingPos.y * 2 / 3));
                prize.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one*2,Mathf.SmoothStep(0, 1, t));
                
            }
            if (prize.transform.localPosition.y < startingPos.y / 3)
            {
                speed -= .3f;
            }
            yield return new WaitForSeconds(0.001f * Time.deltaTime * 2);
        }

        button.SetActive(true);
        buttonText.text = "Claim";
        starInstance = Instantiate(starburst, wheel.transform.position, Quaternion.identity, prize.transform);
        Canvas starCanvas = starInstance.GetComponent<Canvas>();
        starCanvas.overrideSorting = true;

        while (claiming)
        {
            starInstance.transform.Rotate(0, 0, .5f);
            yield return new WaitForSeconds(0.001f * Time.deltaTime * 2);
        }
        
    }

    // Prepares the wheel for it's next spin
    private void NewSpin()
    {
        claiming = false;
        Destroy(starInstance);
        prize.transform.localScale = Vector3.one;
        prize.transform.localPosition = startingPos;
        foreach(GameObject hidable in hideables)
        {
            hidable.SetActive(true);
        }
        prize.transform.SetParent(wheel.transform);
        buttonText.text = "Play On";
    }

}
