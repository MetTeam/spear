# spear
Spear轻量级微服务框架，高扩展性，目前已支持TCP、HTTP协议，采用Consul作为服务注册与发现组件，TCP协议采用DotNetty底层实现，HTTP协议采用ASP.NET CORE MVC实现。

| Package Name |  NuGet | Downloads | |
|--------------|  ------- |  ---- | -- |
| Spear.ProxyGenerator | [![nuget](https://img.shields.io/nuget/v/Spear.ProxyGenerator.svg?style=flat-square)](https://www.nuget.org/packages/Spear.ProxyGenerator) | [![stats](https://img.shields.io/nuget/dt/Spear.ProxyGenerator.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.ProxyGenerator?groupby=Version) |
| Spear.Core | [![nuget](https://img.shields.io/nuget/v/Spear.Core.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Core) | [![stats](https://img.shields.io/nuget/dt/Spear.Core.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Core?groupby=Version) | [Wiki](https://github.com/shoy160/spear/wiki) |
| Spear.Codec.MessagePack | [![nuget](https://img.shields.io/nuget/v/Spear.Codec.MessagePack.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Codec.MessagePack) | [![stats](https://img.shields.io/nuget/dt/Spear.Codec.MessagePack.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Codec.MessagePack?groupby=Version) | 
| Spear.Codec.ProtoBuffer | [![nuget](https://img.shields.io/nuget/v/Spear.Codec.ProtoBuffer.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Codec.ProtoBuffer) | [![stats](https://img.shields.io/nuget/dt/Spear.Codec.ProtoBuffer.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Codec.ProtoBuffer?groupby=Version) | 
| Spear.Consul | [![nuget](https://img.shields.io/nuget/v/Spear.Consul.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Consul) | [![stats](https://img.shields.io/nuget/dt/Spear.Consul.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Consul?groupby=Version) |
| Spear.Nacos | [![nuget](https://img.shields.io/nuget/v/Spear.Nacos.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Nacos) | [![stats](https://img.shields.io/nuget/dt/Spear.Nacos.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Nacos?groupby=Version) |
| Spear.Protocol.Http | [![nuget](https://img.shields.io/nuget/v/Spear.Protocol.Http.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Protocol.Http) | [![stats](https://img.shields.io/nuget/dt/Spear.Protocol.Http.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Protocol.Http?groupby=Version) |
| Spear.Protocol.Tcp | [![nuget](https://img.shields.io/nuget/v/Spear.Protocol.Tcp.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Protocol.Tcp) | [![stats](https://img.shields.io/nuget/dt/Spear.Protocol.Tcp.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Protocol.Tcp?groupby=Version) |
| Spear.Protocol.WebSocket | [![nuget](https://img.shields.io/nuget/v/Spear.Protocol.WebSocket.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Protocol.WebSocket) | [![stats](https://img.shields.io/nuget/dt/Spear.Protocol.WebSocket.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Protocol.WebSocket?groupby=Version) |
| Spear.Protocol.Grpc | [![nuget](https://img.shields.io/nuget/v/Spear.Protocol.Grpc.svg?style=flat-square)](https://www.nuget.org/packages/Spear.Protocol.Grpc) | [![stats](https://img.shields.io/nuget/dt/Spear.Protocol.Grpc.svg?style=flat-square)](https://www.nuget.org/stats/packages/Spear.Protocol.Grpc?groupby=Version) |

### Contracts
``` c#
[ServiceRoute("test")] //自定义路由键
public interface ITestContract : ISpearService
{
    Task Notice(string name);
    Task<string> Get(string name);
}
```
### Server
``` c#
var services = new MicroBuilder();
//服务协议
var protocol = ServiceProtocol.Tcp;
services.AddMicroService(builder =>
{
    //服务端需指定编解码器和使用协议
    builder
        .AddJsonCoder()             //Json编解码
        //.AddMessagePackCodec()    //MessagePack
        //.AddProtoBufCodec()       //ProtoBuf
        .AddSession()
        //.AddNacos()
        .AddConsul("http://127.0.0.1:8500"); //Consul服务注册与发现
    switch (protocol)
    {
        case ServiceProtocol.Tcp:
            builder.AddTcpProtocol();       //TCP
            break;
        case ServiceProtocol.Http:
            builder.AddHttpProtocol();      //Http
            break;
        case ServiceProtocol.Ws:
            builder.AddWebSocketProtocol(); //WebSocket
            break;
        case ServiceProtocol.Grpc:
            builder.AddGrpcProtocol();      //GRpc
            break;
    }
});

services.AddTransient<ITestContract, TestService>();

var provider = services.BuildServiceProvider();

provider.UseMicroService(address =>
{
    address.Service = "192.168.1.xx";   //服务注册地址,需要保持与客户端的网络访问
    address.Host = "localhost";         //主机地址
    address.Port = 5001;                //端口地址
    address.Weight = 1.5;               //服务权重
    address.Gzip = true;                //是否启用GZip压缩
});
```

### Client
``` c#
var services = new MicroBuilder()
    .AddMicroClient(builder =>
    {
        //支持多编解码&多协议
        builder
            .AddJsonCodec()
            .AddMessagePackCodec()
            .AddProtoBufCodec()
            .AddHttpProtocol()          //Http
            .AddTcpProtocol()           //TCP
            .AddWebSocketProtocol()     //WebSocket
            .AddGrpcProtocol()          //GRpc
            .AddSession()
            //.AddNacos()
            .AddConsul("http://127.0.0.1:8500");
    });
var provider = services.BuildServiceProvider();
var proxy = provider.GetService<IProxyFactory>();
var service = proxy.Create<ITestContract>();
```

### BenchMark
#### Protocol:Tcp,Codec:Json,Gzip:False
![image](docs/images/benchmark-0-0-0.png)

#### Protocol:Tcp,Codec:Json,Gzip:True
![image](docs/images/benchmark-0-0-1.png)

#### Protocol:Tcp,Codec:MessagePack,Gzip:True
![image](docs/images/benchmark-0-1-1.png)

#### Protocol:Tcp,Codec:ProtoBuf,Gzip:True
![image](docs/images/benchmark-0-2-1.png)

#### Protocol:Http,Codec:Json,Gzip:False
![image](docs/images/benchmark-1-0-0.png)

#### Protocol:Http,Codec:Json,Gzip:True
![image](docs/images/benchmark-1-0-1.png)

#### Protocol:Http,Codec:MessagePack,Gzip:True
![image](docs/images/benchmark-1-1-1.png)

#### Protocol:Http,Codec:ProtoBuf,Gzip:True
![image](docs/images/benchmark-1-2-1.png)

#### Protocol:WebSocket,Codec:Json,Gzip:False
![image](docs/images/benchmark-2-0-0.png)

#### Protocol:WebSocket,Codec:Json,Gzip:True
![image](docs/images/benchmark-2-0-1.png)

#### Protocol:WebSocket,Codec:MessagePack,Gzip:True
![image](docs/images/benchmark-2-1-1.png)

#### Protocol:WebSocket,Codec:ProtoBuf,Gzip:True
![image](docs/images/benchmark-2-2-1.png)

#### Protocol:GRpc
![image](docs/images/benchmark-4-0-0.png)
