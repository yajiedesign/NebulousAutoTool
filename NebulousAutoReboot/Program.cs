

using System.Diagnostics;
using Newtonsoft.Json;
using SteamQueryNet;
using SteamQueryNet.Interfaces;

namespace NebulousAutoReboot;

public static class Program
{
    static void Main(string[] args)
    {
        //读取参数获取配置文件路径
        if ( args.Length == 0   )
        {
            Console.WriteLine("未指定配置文件路径");
            return;
        }
        var configPath = args[0];

        if (args.Length == 2 && args[1] == "create")
        {
            Console.WriteLine("创建配置文件");
            //创建配置文件
            var configCreate = new Config
            {
                ServerIp = "127.0.0.1",
                QueryPort = 27015,
                ServiceName = "nebulous",
                ServerConfigPath = "/home"
            };
            var configJson = JsonConvert.SerializeObject(configCreate);
            File.WriteAllText(configPath, configJson);
            Console.WriteLine("配置文件创建成功");
            return;
        }

        //读取配置文件,配置文件为json格式
        var config = File.ReadAllText(configPath);
        //反序列化配置文件
        var configObject = JsonConvert.DeserializeObject<Config>(config);
        if(configObject == null)
        {
            Console.WriteLine("配置文件读取失败");
            return;
        }
        //检查服务是否运行
        var serviceName = configObject.ServiceName;
        
        var checkService = Process.Start("systemctl", $"is-active {serviceName} --quiet");
        checkService.WaitForExit();
        if(checkService.ExitCode != 0)
        {
            //如果服务未运行,则启动服务
            Console.WriteLine("服务未运行,启动服务");
            var startService = Process.Start("systemctl", $"start {serviceName}");
            startService.WaitForExit();
            Console.WriteLine("服务已启动");
            return;
        }
        
        string serverIp =configObject.ServerIp;
        ushort serverPort =(ushort) configObject.QueryPort;
        IServerQuery serverQuery = new ServerQuery(serverIp, serverPort);
        //获取服务器状态
        var serverInfo = serverQuery.GetServerInfo();

        //获取服务器玩家数量
        var playerCount = serverInfo.Players;
        if(playerCount==0)
        {
            //如果玩家数量为0,则重启服务器
            Console.WriteLine("玩家数量为0,重启服务器");
            //停止服务
            var stopService = Process.Start("systemctl", $"stop {serviceName}");
            stopService.WaitForExit();
            //启动服务
            var startService = Process.Start("systemctl", $"start {serviceName}");
            startService.WaitForExit();
            Console.WriteLine("服务已重启");
            return;
        }
        
        Console.WriteLine("玩家数量不为0,发送服务器重启指令");
        //如果玩家数量不为0,发送服务器重启指令
        var serverConfigPath = configObject.ServerConfigPath;
        string command = "<ServerCommandFile>\n" +
                         "    <Command>ScheduleRestart</Command>\n" +
                         "    <Message>AutoReboot: Server would reboot after the end of this match as part of the hourly reboot routine.</Message>\n" +
                         "</ServerCommandFile>";
 
        var commandPath = Path.Combine(serverConfigPath, "ServerCommand.xml");
        File.WriteAllText(commandPath, command);
        //设置文件权限
        var chmodCommand = Process.Start("chmod", $"666 {commandPath}");
        chmodCommand.WaitForExit();
        Console.WriteLine("指令已发送");
    }
}