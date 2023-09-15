using ENet;
using WindowsInput.Native;
using WindowsInput;

internal class HandleInput : IGeneralControlsPacket, IEnterPacket
{
    InputSimulator sim = new InputSimulator();

    bool rightIsPressed;
    bool leftIsPressed;
    bool upIsPressed;
    bool downIsPressed;
    bool fireIsPressed;

    public HandleInput()
    {
        MainThreadNetcode.timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        //DONT PRESS KEYS WHEN GAME IS RESETING
        if (GameMemory.health <= 26)
        {
            sim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            sim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            sim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            sim.Keyboard.KeyUp(VirtualKeyCode.VK_X);
        }

    }

    public void EnterPacketRecieved(Event netEvent, EnterPacket enterPacket)
    {
        if (enterPacket.enterKey)
        {
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }
    }

    public void GeneralControlsPacketRecieved(Event netEvent, GeneralControlsPacket generalControlsPacket)
    {
        if (GameMemory.health > 26)
        {
            if (generalControlsPacket.turn[0])
            {
                if (!rightIsPressed)
                {
                    // turn right
                    sim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
                    rightIsPressed = true;
                }

            }
            else
            {
                if (rightIsPressed)
                {
                    sim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
                    rightIsPressed = false;
                }
            }
            if (generalControlsPacket.turn[1])
            {
                if (!leftIsPressed)
                {
                    // turn left
                    sim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
                    leftIsPressed = true;
                }
            }
            else
            {
                if (leftIsPressed)
                {
                    sim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
                    leftIsPressed = false;
                }
            }

            if (generalControlsPacket.speed[0])
            {
                if (!upIsPressed)
                {
                    // accelerate
                    sim.Keyboard.KeyDown(VirtualKeyCode.UP);
                    upIsPressed = true;
                }
            }
            else
            {
                if (upIsPressed)
                {
                    sim.Keyboard.KeyUp(VirtualKeyCode.UP);
                    upIsPressed = false;
                }
            }

            if (generalControlsPacket.speed[1])
            {
                if (!downIsPressed)
                {
                    //slow down
                    sim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
                    downIsPressed = true;
                }
            }
            else
            {
                if (downIsPressed)
                {
                    sim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
                    downIsPressed = false;
                }
            }

            if (generalControlsPacket.isFiring[0])
            {
                if (!fireIsPressed)
                {
                    //shoot
                    sim.Keyboard.KeyDown(VirtualKeyCode.VK_X);
                    fireIsPressed = true;
                }
            }
            else
            {
                if (fireIsPressed)
                {
                    sim.Keyboard.KeyUp(VirtualKeyCode.VK_X);
                    fireIsPressed = false;
                }
            }
        }
    }

    ~HandleInput()
    {
        MainThreadNetcode.timer.Elapsed -= Timer_Elapsed;
    }
}

