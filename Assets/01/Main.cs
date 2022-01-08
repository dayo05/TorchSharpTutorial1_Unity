using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace _01
{
    public class Main : MonoBehaviour
    {
        private Queue<MlResult> DataFromServer = new Queue<MlResult>();
        private object dataQueueLock = new object();
        public GameObject line;
        private GameObject current;

        private Thread receiveTask;

        void PushQueue(MlResult r)
        {
            lock (dataQueueLock)
            {
                DataFromServer.Enqueue(r);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            current = Instantiate(line);
            current.transform.position = Vector3.zero;
            current.GetComponent<SpriteRenderer>().color = Color.red;

            /*
            receiveTask = new Thread(() =>
            {
                var client = new TcpClient("127.0.0.1", 8080);
                var stream = client.GetStream();
                while (true)
                {
                    if (!client.Connected) break;
                    if (!stream.DataAvailable) continue;
                    var buf = new byte[1000];
                    stream.Read(buf, 0, buf.Length);
                    var s = Encoding.UTF8.GetString(buf).Split('\n');
                    foreach (var x in s)
                    {
                        if (x == "end") return;
                        var k = x.Split(',');
                        if (k.Length < 2) continue;
                        PushQueue(new MlResult
                        {
                            w = float.Parse(k[0]),
                            b = float.Parse(k[1])
                        });
                    }
                }

                Debug.Log("Finished");
            });
            receiveTask.Start();
            */

            StartCoroutine(ReceiveFromServer());
            StartCoroutine(RenderCoroutine());
            Debug.Log("Start finished");
        }

        void OnApplicationQuit()
        {
            //receiveTask.Interrupt();
        }

        IEnumerator RenderCoroutine()
        {
            while (true)
            {
                lock (dataQueueLock)
                {
                    if (DataFromServer.Count == 0)
                    {
                        yield return null;
                        continue;
                    }

                    var data = DataFromServer.Dequeue();
                    Debug.Log($"Render: {data.w} {data.b}");
                    current.GetComponent<SpriteRenderer>().color = Color.black;
                    current = Instantiate(line);
                    current.transform.position = new Vector3(0, data.b, 0);
                    current.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan(data.w) * Mathf.Rad2Deg);
                    current.GetComponent<SpriteRenderer>().color = Color.red;
                }
                yield return new WaitForSecondsRealtime(.5f);
            }
        }

        private bool isFinished = false;

        IEnumerator ReceiveFromServer()
        {
            if (isFinished)
                yield break;
            var client = new TcpClient("127.0.0.1", 8080);
            var stream = client.GetStream();
            while (true)
            {
                if (!client.Connected) break;
                if (!stream.DataAvailable)
                {
                    yield return null;
                    continue;
                }
                var buf = new byte[1000];
                stream.Read(buf, 0, buf.Length);
                var s = Encoding.UTF8.GetString(buf).Split('\n');
                foreach (var x in s)
                {
                    if (x == "end") break;
                    var k = x.Split(',');
                    if (k.Length < 2)
                    {
                        yield return null;
                        continue;
                    }
                    PushQueue(new MlResult
                    {
                        w = float.Parse(k[0]),
                        b = float.Parse(k[1])
                    });
                }

                yield return null;
            }

            isFinished = true;
            Debug.Log("Finished");
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
