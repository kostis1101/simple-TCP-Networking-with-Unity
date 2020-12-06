using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class sendData : MonoBehaviour
{

    TcpClient client = new TcpClient("127.0.0.1", 5050);

    private void Start()
    {
        SendData($"add player:{client.Client.RemoteEndPoint}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendData("hello there");
        }
    }
    
    void SendData(string message)
    {
        int byteCount = Encoding.ASCII.GetByteCount(message + 1);
        byte[] sendData = new byte[byteCount];
        sendData = Encoding.ASCII.GetBytes(message);

        NetworkStream stream = client.GetStream();
        if (message == "!STOP")
        {
            stream.Close();
            client.Close();
            Console.ReadKey();
            return;
        }
        stream.Write(sendData, 0, sendData.Length);
        //print("sending data to server...");

        StreamReader sr = new StreamReader(stream);
        string responce = sr.ReadLine();
        print($"message received from server: {responce}");
    }
}
