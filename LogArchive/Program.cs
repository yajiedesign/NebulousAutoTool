using Newtonsoft.Json;

namespace LogArchive;

public static class Program
{
    static void Main(string[] args)
    {
        //读取参数获取配置文件路径
        Console.WriteLine("开始归档日志");
        var configPath = args[0];
        //检查配置文件是否存在
        if(!File.Exists(configPath))
        {
            Console.WriteLine("配置文件不存在");
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
        
        //日志路径
        var logPath = Path.Combine(configObject.LogDir,$"server_{configObject.ServerName}.log");
        
        //检查日志文件是否存在
        if(!File.Exists(logPath))
        {
            Console.WriteLine("日志文件不存在");
            return;
        }
        //获取日志文件修改时间
        var logCreationTime = File.GetCreationTime(logPath);
        logCreationTime = logCreationTime.AddHours(8);
        //日志归档路径
        var logArchivePath = Path.Combine(configObject.LogArchiveDir,configObject.ServerName ,$"server_{configObject.ServerName}_{logCreationTime:yyyyMMddHHmmss}.log");
        Console.WriteLine($"日志归档路径:{logArchivePath}");
        //如果日志文件不存在,则退出
        if(!File.Exists(logPath))
        {
            Console.WriteLine("日志文件不存在");
            return;
        }
        //如果日志归档文件夹不存在,则创建
        if(!Directory.Exists(Path.GetDirectoryName(logArchivePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logArchivePath));
        }
        Console.WriteLine("开始归档日志");
        //将日志文件移动到归档文件夹
        try
        {
            File.Move(logPath,logArchivePath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        Console.WriteLine("日志归档完成");
    }
}