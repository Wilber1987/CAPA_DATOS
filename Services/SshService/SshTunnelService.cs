using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using System;

public class SshTunnelService
{
	private SshClient _sshClient;
	private ForwardedPortLocal _forwardedPort;
	private readonly IConfigurationRoot _configuration;

	public SshTunnelService()
	{
		// Cargar las configuraciones automáticamente al crear la instancia
		_configuration = LoadConfiguration();
	}

	private IConfigurationRoot LoadConfiguration()
	{
		return new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())  // Ruta de trabajo actual
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // Cargar el archivo appsettings.json
			.Build(); // Construir la configuración
	}
	
	public SshClient GetSshClient(string connectionName)
	{
		var sshSettings = _configuration.GetSection($"ConnectionStrings:SSHConnection{connectionName}");
		var host = sshSettings["HostName"];
		var username = sshSettings["UserName"];
		var password = sshSettings["Password"];
		var port = int.Parse(sshSettings["Port"]);
		var _sshClient = new SshClient(host, port, username, password);
		return _sshClient;
	}
	public ForwardedPortLocal GetForwardedPort(string connectionName, SshClient _sshClient)
	{
		var mysqlSettings = _configuration.GetSection($"ConnectionStrings:MySQLConnection{connectionName}");
		var localPort = 3306;
		var remoteHost = mysqlSettings["Server"];
		var remotePort = int.Parse(mysqlSettings["Port"]);
		var _forwardedPort = new ForwardedPortLocal("127.0.0.1", (uint)localPort, remoteHost, (uint)remotePort);
		_sshClient.AddForwardedPort(_forwardedPort);
		return _forwardedPort;
	}
	

	public void OpenTunnel(string connectionName)
	{
		// Cargar las configuraciones específicas para el túnel SSH
		var sshSettings = _configuration.GetSection($"ConnectionStrings:SSHConnection{connectionName}");
		var host = sshSettings["HostName"];
		var username = sshSettings["UserName"];
		var password = sshSettings["Password"];
		var port = int.Parse(sshSettings["Port"]);

		_sshClient = new SshClient(host, port, username, password);
		_sshClient.Connect();

		if (_sshClient.IsConnected)
		{
			Console.WriteLine($"SSH tunnel to {connectionName} opened successfully.");
		}

		var mysqlSettings = _configuration.GetSection($"ConnectionStrings:MySQLConnection{connectionName}");
		var localPort = 3306;
		var remoteHost = mysqlSettings["Server"];
		var remotePort = int.Parse(mysqlSettings["Port"]);

		_forwardedPort = new ForwardedPortLocal("127.0.0.1", (uint)localPort, remoteHost, (uint)remotePort);
		_sshClient.AddForwardedPort(_forwardedPort);
		_forwardedPort.Start();
	}

	public void CloseTunnel()
	{
		if (_forwardedPort != null && _forwardedPort.IsStarted)
		{
			_forwardedPort.Stop();
		}

		if (_sshClient != null && _sshClient.IsConnected)
		{
			_sshClient.Disconnect();
			Console.WriteLine("SSH tunnel closed.");
		}
	}
}
