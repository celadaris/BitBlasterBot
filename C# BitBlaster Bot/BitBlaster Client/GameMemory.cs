using gh;
using System;
using System.Diagnostics;

public class GameMemory
{
    Process proc = Process.GetProcessesByName("BitBlasterXL")[0];

    public static byte health;
    public static int score;

    public GameMemory()
    {
        MainThreadNetcode.timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        health = (byte)GetHealth();
        score = GetScore();

        PacketConfig packetConfig = new PacketConfig()
        {
            packetFlag = ENet.PacketFlags.None,
        };

        MemoryPacket packet = new MemoryPacket()
        {
            packetID = 4,
            life = health,
            score = score
        };

        //send score & health to server
        Courier.SendMemoryPacket(packetConfig, packet);
    }

    int GetScore()
    {
        var hProc = ghAPI.OpenProcess(ghAPI.ProcessAccessFlags.All, false, proc.Id);
        var modBase = ghAPI.GetModuleBaseAddress(proc.Id, "UnityPlayer.dll");

        var scoreAddr = ghAPI.FindDMAAddy(hProc, modBase + 0x01993010, new int[] { 0x10, 0x108, 0x0, 0xD0, 0x8, 0x60, 0xC8 });

        byte[] scoredata = new byte[4];

        ghAPI.ReadProcessMemory(hProc, scoreAddr, scoredata, 4, out _);

        return BitConverter.ToInt32(scoredata, 0);
    }

    int GetHealth()
    {
        var hProc = ghAPI.OpenProcess(ghAPI.ProcessAccessFlags.All, false, proc.Id);
        var modBase = ghAPI.GetModuleBaseAddress(proc.Id, "UnityPlayer.dll");

        var healthAddr = ghAPI.FindDMAAddy(hProc, modBase + 0x01ACA468, new int[] { 0x490, 0x10, 0xE0, 0x0, 0xB8, 0xF0, 0xE10 });

        byte[] healthdata = new byte[4];

        ghAPI.ReadProcessMemory(hProc, healthAddr, healthdata, 4, out _);

        return BitConverter.ToInt32(healthdata, 0);
    }

    ~GameMemory()
    {
        MainThreadNetcode.timer.Elapsed -= Timer_Elapsed;
    }
}
