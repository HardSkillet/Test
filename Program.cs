using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace SocketTcpClient
{
    class Test
    {
        static int numberOfRequestsExecuted = 0;
        static int port = 2013;
        static string address = "88.212.241.115";    //Порт и IP-адресс
        static IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);
        static Stack<int> errors; // Стек для отлова ошибок
        const int n = 2018;
        static bool[] answerRecived = new bool[n];         //Получили значение от сервера 
        static int[] arrayOfResponses = new int[n];   //Массив с обработанными ответами от сервера
        static Regex regex = new Regex(@"\d*");    //Регулярное выражение для поиска чисел

        static void SendMessage(ref Socket socket, string message)
        {
            Encoding encodingType = Encoding.GetEncoding("US-ASCII");
            Byte[] data = encodingType.GetBytes(message);
            socket.Send(data);
        }
        static StringBuilder RecieveMessage(ref Socket socket)
        {
            Byte[] data = new byte[256];
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encodingType = Encoding.GetEncoding("koi8-r");

            do
            {
                bytes = socket.Receive(data, data.Length, 0);
                builder.Append(encodingType.GetString(data, 0, bytes));
            }
            while (builder[builder.Length - 1] != '\n' && bytes > 0);
            return builder;
        }
        static int ParseString(string response)
        {
            int answer = 0;                              //Полученное число
            MatchCollection matches = regex.Matches(response);      //Набор успешных совпадений
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                    if (match.Value != "") answer = int.Parse(match.Value);
            }
            return answer;
        }    //Парсим строку, пришедшую с сервера
        static void GetServerAnswer(int number)
        {
            while (!answerRecived[number - 1])
            {
                Socket socket = null;
                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    int answer = 0;
                    socket.Connect(ipPoint);

                    string message = number.ToString() + '\n';
                    SendMessage(ref socket, message);
                    StringBuilder builder = RecieveMessage(ref socket);
                    answer = ParseString(builder.ToString());
                    if (answer != 0 && (builder[builder.Length - 1] == '\n' || builder[builder.Length - 1] == ' ' || builder[builder.Length - 1] == '.'))
                    {
                        arrayOfResponses[number - 1] = answer;
                        answerRecived[number - 1] = true;
                    }
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch
                {
                    socket.Dispose();
                    errors.Push(number);
                    return;
                }
                finally {
                    socket.Dispose();
                }
            }
        }
        static void WaitingForAllAnswers(int leftBorder, int rightBorder)
        {
            int numberOfSeconds = 0;
            while (true)
            {
                Console.Write("Прошло " + numberOfSeconds + " секунд -> ");
                Thread.Sleep(1000);
                numberOfSeconds++;
                numberOfRequestsExecuted = 0;
                while (errors.Count != 0)
                {
                    Thread.Sleep(100);
                    int temp = errors.Pop();
                    GetServerAnswerAsync(temp);
                }
                for (int i = leftBorder; i < rightBorder; ++i)
                    if (answerRecived[i]) numberOfRequestsExecuted++;
                if (numberOfRequestsExecuted == rightBorder - leftBorder) break;
                else
                {
                    int a = numberOfRequestsExecuted * 100 / (rightBorder - leftBorder);
                    Console.Write(a);
                    Console.WriteLine(" %");
                }
            }
        }
        static void SendAnswer(double answer)
        {
            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipPoint);

                string message = "Check " + answer.ToString() + "\n";
                SendMessage(ref socket, message);
                StringBuilder builder = RecieveMessage(ref socket);
                Console.WriteLine(builder.ToString());
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch
            {
                socket.Dispose();
            }
        }
        static async void GetServerAnswerAsync(int number)
        {
            await Task.Run(() => GetServerAnswer(number));
        }

        static void Main(string[] args)
        {
            for (int j = 0; j < 4; ++j)
            {
                errors = new Stack<int>();
                int leftBorder = j * 505;
                int rightBorder;

                if (j == 3) rightBorder = 2018;
                else rightBorder = (j + 1) * 505;

                Console.WriteLine("Начало: " + j);
                for (int i = leftBorder + 1; i <= rightBorder; ++i) GetServerAnswerAsync(i);

                WaitingForAllAnswers(leftBorder, rightBorder);

                Console.WriteLine("Закончил " + (j + 1));
            }
            Array.Sort(arrayOfResponses);

            double answer = arrayOfResponses[1009] + arrayOfResponses[1008];
            answer /= 2;

            SendAnswer(answer);
            Console.ReadKey();
        }
    }
}