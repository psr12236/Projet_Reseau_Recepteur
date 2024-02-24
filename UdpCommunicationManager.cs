using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

public class UdpCommunicationManager
{
    private bool _continueReceiving = true;
    private Socket socket;
    private Dictionary<ushort, Paquet> sentPackets; // Pour suivre les paquets envoyés qui attendent un ACK
    private Timer retransmissionTimer; // Timer pour la retransmission
    private IPEndPoint remoteEndPoint;

    public UdpCommunicationManager(int listeningPort)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, listeningPort);
        socket.Bind(localEndPoint); // Associez le socket à l'adresse IP locale et au port d'écoute
    }

    public void StartRetransmissionTimer()
    {
        retransmissionTimer = new Timer(Retransmit, null, 0, 1000); // Ajustez la période selon vos besoins
    }

    private void Retransmit(object state)
    {
        var packetsToRetransmit = new List<ushort>();

        // Identifier les paquets non acquittés
        foreach (var packetEntry in sentPackets)
        {
            // Ajoutez votre logique pour déterminer si un paquet doit être retransmis,
            // par exemple, en fonction du temps écoulé depuis l'envoi.
            packetsToRetransmit.Add(packetEntry.Key);
        }

        // Retransmettre les paquets
        foreach (var sequenceNumber in packetsToRetransmit)
        {
            if (sentPackets.TryGetValue(sequenceNumber, out Paquet paquet))
            {
                Console.WriteLine($"Retransmitting packet {sequenceNumber}");
                SendPacket(paquet);
            }
        }
    }
    public void ReceiveFile(string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            while (_continueReceiving)
            {
                bool receiving = true;

                while (receiving)
                {
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] buffer = new byte[1024 + 5]; // Taille de paquet + en-têtes
                    int received = socket.ReceiveFrom(buffer, ref remoteEP);

                    if (received > 5) // Si nous avons plus que les en-têtes
                    {
                        Paquet paquet = new DataSerialise().DeserializePaquet(buffer);
                        if (paquet.Data.Length > 0)
                        {
                            fileStream.Write(paquet.Data, 0, paquet.Data.Length);
                        }
                        // Envoyer un ACK si votre protocole le requiert
                        // SendAck(paquet.PacketSequenceNumber, remoteEP);
                    }
                    else
                    {
                        receiving = false; // Aucune donnée reçue, supposez que l'envoi est terminé
                    }
                }
        }
    }
}


    public void SendPacket(Paquet paquet)
    {
        var data = new DataSerialise().SerializePaquet(paquet);
        socket.SendTo(data, remoteEndPoint);

        if (!paquet.IsAck) // Si ce n'est pas un ACK, attendez un ACK en retour
        {
            sentPackets.Add(paquet.PacketSequenceNumber, paquet);
        }
    }

    public void ReceivePacket()
    {
        var buffer = new byte[1024]; // Ajustez la taille selon vos besoins
        EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            int received = socket.ReceiveFrom(buffer, ref senderRemote);
            var paquet = new DataSerialise().DeserializePaquet(buffer);

            if (paquet.IsAck)
            {
                // Traitez l'ACK reçu
                if (sentPackets.ContainsKey(paquet.AckFor))
                {
                    // ACK reçu, retirez le paquet de sentPackets
                    sentPackets.Remove(paquet.AckFor);
                }
            }
            else
            {
                // Traitez le paquet reçu et envoyez un ACK en réponse si nécessaire
                SendAck(paquet.PacketSequenceNumber, senderRemote);
            }
        }
    }

    private void SendAck(ushort packetSequenceNumber, EndPoint remote)
    {
        Paquet ackPacket = new Paquet
        {
            IsAck = true,
            AckFor = packetSequenceNumber,
            // Assurez-vous de configurer correctement les autres champs nécessaires
        };

        // Serialisez et envoyez le paquet ACK
        var ackData = new DataSerialise().SerializePaquet(ackPacket);
        socket.SendTo(ackData, remote);
    }
    public void StopReceiving()
    {
        _continueReceiving = false;
        socket.Close(); // Ferme le socket pour arrêter de recevoir des données.
    }

}
