using DisruptorUnity3d;
using System.Runtime.InteropServices;

public class MainThreadNetcode
{
    public static RingBuffer<GeneralControlsPacketContainer> recvGeneralControlsPacketToGame { get; set; }
    public static RingBuffer<EnterPacketContainer> recvEnterPacketToGame { get; set; }
    public static System.Timers.Timer timer { get; set; }
    HandleInput handleInput;

    public MainThreadNetcode()
    {
        recvGeneralControlsPacketToGame = new RingBuffer<GeneralControlsPacketContainer>(100);
        recvEnterPacketToGame = new RingBuffer<EnterPacketContainer>(100);
        timer = new System.Timers.Timer(16.7);
        timer.Start();
        timer.Elapsed += Timer_Elapsed;
        GameMemory gameMemory = new GameMemory();
        handleInput = new HandleInput();
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        while (recvGeneralControlsPacketToGame.Count > 0)
        {
            GeneralControlsPacketContainer objects = recvGeneralControlsPacketToGame.Dequeue();

            //convert IntPtr to struct
            ENet.Event recvEvent;
            recvEvent = (ENet.Event)Marshal.PtrToStructure(objects.packetConfig, typeof(ENet.Event));

            //free the IntPtr memory
            Marshal.FreeHGlobal(objects.packetConfig);

            handleInput.GeneralControlsPacketRecieved(recvEvent, objects.generalControlsPacket);
        }
        while (recvEnterPacketToGame.Count > 0)
        {
            EnterPacketContainer objects = recvEnterPacketToGame.Dequeue();

            //convert IntPtr to struct
            ENet.Event recvEvent;
            recvEvent = (ENet.Event)Marshal.PtrToStructure(objects.packetConfig, typeof(ENet.Event));

            //free the IntPtr memory
            Marshal.FreeHGlobal(objects.packetConfig);

            handleInput.EnterPacketRecieved(recvEvent, objects.enterPacket);
        }
    }

    ~MainThreadNetcode()
    {
        timer.Elapsed -= Timer_Elapsed;
    }
}