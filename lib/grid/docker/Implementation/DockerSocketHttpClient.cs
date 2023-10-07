namespace Grid;

using System;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Net.Sockets;

using Docker.DotNet;

using Microsoft.Net.Http.Client;

/// <summary>
/// Represents a HTTP Client for the Docker Socket.
/// </summary>
public class DockerSocketHttpClient
{
    private class UnixSocketEndPoint : EndPoint
    {
        private string filename;

        public UnixSocketEndPoint(string filename)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            if (filename == "") throw new ArgumentException("Cannot be empty.", nameof(filename));

            this.filename = filename;
        }

        public string Filename
        {
            get => filename;
            set => filename = value;
        }

        public override AddressFamily AddressFamily => AddressFamily.Unix;

        public override EndPoint Create(SocketAddress socketAddress)
        {
            if (socketAddress.Size == 2)
                return new UnixSocketEndPoint("a")
                {
                    filename = ""
                };

            var socketIdx = socketAddress.Size - 2;
            var array = new byte[socketIdx];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = socketAddress[i + 2];

                if (array[i] == 0)
                {
                    socketIdx = i;
                    break;
                }
            }

            return new UnixSocketEndPoint(Encoding.UTF8.GetString(array, 0, socketIdx));
        }

        public override SocketAddress Serialize()
        {
            var data = Encoding.UTF8.GetBytes(filename);
            var addr = new SocketAddress(AddressFamily, 2 + data.Length + 1);

            for (int i = 0; i < data.Length; i++)
                addr[2 + i] = data[i];

            addr[2 + data.Length] = 0;

            return addr;
        }

        public override string ToString() => filename;
        public override int GetHashCode() => filename.GetHashCode();
        public override bool Equals(object o) => o is UnixSocketEndPoint unixSocketEndPoint && unixSocketEndPoint.filename == filename;

    }

    /// <summary>
    /// Create a new instance of <see cref="DockerSocketHttpClient"/>
    /// </summary>
    /// <param name="dockerClient">The actual docker client.</param>
    /// <returns>The HTTP Client.</returns>
    public static HttpClient CreateClient(DockerClient dockerClient) 
        => new(
            dockerClient.Configuration.Credentials.GetHandler(
                new ManagedHandler(
                    async (host, port, token) =>
                    {
                        var sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);

                        await sock.ConnectAsync(new UnixSocketEndPoint(dockerClient.Configuration.EndpointBaseUri.LocalPath));

                        return sock;
                    }
                )
            ),
            true
        );
}
