using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using System.Collections;

public class RLScript : Agent, IMemoryPacket
{
    [SerializeField] Courier courier;

    int previousScore = 0;
    int previousHealth = 26;

    bool[] turn;
    bool[] speed;
    bool[] isFiring;
    bool isPlaying;
    bool isWaiting;

    private void Start()
    {
        turn = new bool[2];
        speed = new bool[2];
        isFiring = new bool[2];
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        PacketConfig packetConfig = new PacketConfig()
        {
            packetFlag = ENet.PacketFlags.None
        };


        if (actions.DiscreteActions[0] == 0)
        {
            // turn right
            turn[0] = true;
            turn[1] = false;

        }
        else if (actions.DiscreteActions[0] == 1)
        {
            // turn left
            turn[0] = false;
            turn[1] = true;
        }
        else if (actions.DiscreteActions[0] == 2)
        {
            // dont turn
            turn[0] = false;
            turn[1] = false;
        }

        if (actions.DiscreteActions[1] == 0)
        {
            // accelerate
            speed[0] = true;
            speed[1] = false;
        }
        else if (actions.DiscreteActions[1] == 1)
        {
            // slow down
            speed[0] = false;
            speed[1] = true;
        }
        else if (actions.DiscreteActions[1] == 2)
        {
            // constant speed
            speed[0] = false;
            speed[1] = false;
        }

        if (actions.DiscreteActions[2] == 0)
        {
            // shoot
            isFiring[0] = true;
        }
        else if (actions.DiscreteActions[2] == 1)
        {
            // dont shoot
            isFiring[0] = false;
        }

        GeneralControlsPacket packet = new GeneralControlsPacket()
        {
            packetID = 1,
            arraySize = 2,
            turn = turn,
            speed = speed,
            isFiring = isFiring,

        };

        courier.SendGeneralControlsPacket(packetConfig, packet);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> actionSegment = actionsOut.DiscreteActions;


        if (Input.GetKey(KeyCode.RightArrow))
        {
            // turn right
            actionSegment[0] = 0;

        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            // turn left
            actionSegment[0] = 1;
        }
        else
        {
            // dont turn
            actionSegment[0] = 2;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            // accelerate
            actionSegment[1] = 0;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            //slow down
            actionSegment[1] = 1;
        }
        else
        {
            // constant speed
            actionSegment[1] = 2;
        }

        if (Input.GetKey(KeyCode.X))
        {
            //shoot
            actionSegment[2] = 0;
        }
        else
        {
            //dont shoot
            actionSegment[2] = 1;
        }
    }

    public void MemoryPacketRecieved(ENet.Event netEvent, MemoryPacket memoryPacket)
    {
        if(memoryPacket.life > 26) { isPlaying = true; }

        // Calculate score reward
        int scoreDelta = memoryPacket.score - previousScore;
        previousScore = memoryPacket.score;

        // Calculate health reward
        int healthDelta = memoryPacket.life - previousHealth;
        previousHealth = memoryPacket.life;

        if (isPlaying == true)
        {
            if (memoryPacket.life <= 26) { isPlaying = false; }

            // Calculate overall reward
            float score = (scoreDelta / 100) + (healthDelta * 100.5f) + 0.5f;
            SetReward(score);
        }

    }

    void Update()
    {
        if (previousHealth > 26)
        {
            RequestDecision();
        }
        else
        {
            if (isWaiting == false)
            {
                StartCoroutine(SendEnter());
            }
        }
    }

    IEnumerator SendEnter()
    {
        isWaiting = true;

        //call coroutine to do the following
        PacketConfig packetConfig = new PacketConfig()
        {
            packetFlag = ENet.PacketFlags.Reliable
        };

        EnterPacket enterPacket = new EnterPacket()
        {
            packetID = 2,
            enterKey = true
        };

        yield return new WaitForSeconds(0.9f);
        courier.SendEnterPacket(packetConfig, enterPacket);
        yield return new WaitForSeconds(1.3f);
        EndEpisode();
        isWaiting = false;
    }
}
