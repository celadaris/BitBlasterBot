NetworkScript networkScript = new NetworkScript();
LogicScript logicScript = new LogicScript();
Courier courier = new Courier(logicScript);
MainThreadNetcode mainThreadNetcode = new MainThreadNetcode();

ImageRenderer imageRenderer = new ImageRenderer();
HandleConnections handleConnections = new HandleConnections();

Console.ReadLine();
CloseTasks();

void CloseTasks()
{
    NetworkScript.tokenSource.Cancel();
    LogicScript.tokenSource.Cancel();
}