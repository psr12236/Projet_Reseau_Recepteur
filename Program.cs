class Program
{
    static void Main()
    {
        Console.WriteLine("Entrez le port d'écoute:");
        //int listeningPort = int.Parse(Console.ReadLine(11000));
        int listeningPort = 11000;

        Console.WriteLine("Entrez le chemin du fichier où écrire les données reçues:");
        //string filePath = Console.ReadLine();
        string filePath = @"C:\Users\marcl\source\repos\psr12236\Projet_Reseau\Receiver\UDPReceiver\DataR\receivedData";

        Console.WriteLine($"Démarrage de la réception sur le port {listeningPort}. Les données seront écrites dans : {filePath}");
        Console.WriteLine("Appuyez sur 'exit' pour arrêter la réception.");

        UdpCommunicationManager receiver = new UdpCommunicationManager(listeningPort);

        Thread receiverThread = new Thread(() => receiver.ReceiveFile(filePath));
        receiverThread.Start();

        Console.WriteLine("Tapez 'exit' pour arrêter la réception.");
        string userInput;
        do
        {
            userInput = Console.ReadLine();
        } while (userInput.ToLower() != "exit");

        receiver.StopReceiving(); // Cela déclenchera l'arrêt du thread de réception.
        receiverThread.Join(); // Attendre que le thread de réception se termine proprement.
    }
}
