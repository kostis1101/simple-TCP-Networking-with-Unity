using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;

public class dataReceiver : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;
    StreamReader sr;

    List<string> playerNames = new List<string>();
    Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();

    public GameObject playerPrefab;

    List<string> commands = new List<string>();
    CultureInfo culture = CultureInfo.InvariantCulture;

    public float speed = 5f;
    Vector3 last_pos;

    Thread thread;

    void Start()
    {
        client = new TcpClient("127.0.0.1", 5050);
        stream = client.GetStream();
        sr = new StreamReader(stream);

        playerNames.Add(gameObject.name);
        last_pos = gameObject.transform.position;

        SendData($"add player:{gameObject.name}:{transform.position.x}:{transform.position.y}");

        thread = new Thread(new ThreadStart(ReceiveData));
        thread.Start();
    }

    void ReceiveData()
    {
        print("reading data...");
        string data = sr.ReadLine();
        commands.Add(data);
        ReceiveData();
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
        //string responce = sr.ReadLine();
        //print($"message received from server: {responce}");
    }

    void handleRecvData()
    {
        foreach (string command in commands)
        {
            string[] _commands = command.Split(':');

            // Adding players ("add player:<player name>:<x>:<y>")
            if (_commands[0] == "add player")
            {
                string name = _commands[1];
                Vector3 coordinates = new Vector3(float.Parse(_commands[2], culture) / 100, float.Parse(_commands[3], culture) / 100);
                if (!playerNames.Contains(name) && name != gameObject.name)
                {
                    print($"adding player with the name {name}");
                    GameObject player = Instantiate(playerPrefab, coordinates, Quaternion.identity);
                    player.name = name;
                    players.Add(name, player);
                }
                playerNames.Add(name);

            }

            //movement ("move:<player name>:<x>:<y>")
            if (_commands[0] == "move")
            {
                string name = _commands[1];

                Vector2 direction = new Vector2(float.Parse(_commands[2], culture), float.Parse(_commands[3], culture));
                players[name].transform.position = new Vector3(direction.x / 100, direction.y / 100);
            }

            // remove player because of disconnect ("disconnect:<player name>")
            if (_commands[0] == "disconnect")
            {
                print($"{_commands[1]} disconnected...");
            }
        }
        commands.Clear();
    }

    void Update()
    {
        handleRecvData();

        float xMove = Input.GetAxisRaw("Horizontal") * Time.deltaTime * speed;
        float yMove = Input.GetAxisRaw("Vertical") * Time.deltaTime * speed;

        transform.position += new Vector3(xMove, yMove);
        if (last_pos != transform.position)
        {
            SendData($"move:{gameObject.name}:{transform.position.x.ToString("F")}:{transform.position.y.ToString("F")}");
        }

        last_pos = transform.position;
    }
}
