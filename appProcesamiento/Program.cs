using EasyModbus;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace appProcesamiento
{
    class Program
    {
        public static string serverIP = null;
        public static string serverPort = null;

        public static string plcIP = null;
        public static string plcPort = null;

        static void Main(string[] args)
        {

            try
            {
                ObtenerConfig();

                //Inicializo el server donde escucho al cliente scanner
                IPHostEntry host = Dns.GetHostEntry(serverIP);           //USAR IP DE LA COMPUTADORA
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Int32.Parse(serverPort));    //USAR PUERTO DEL SCANNER

                //Inicializo modbus para comunicación con PLC
                ModbusClient modbus = new ModbusClient();
                modbus.IPAddress = plcIP;                              //USAR IP FIJA DEL PLC
                modbus.Port = Convert.ToInt32(plcPort);                //USAR PUERTO DEL PLC

                //Me conecto con el PLC vía modbus
                if (modbus.Connected == false)
                {
                    try
                    {
                        modbus.Connect();
                    }
                    catch (InvalidCastException e)
                    {
                        Console.Write("Error: " + e.Message);
                    }
                }


                //Creo el Socket para conectarme con el scanner      
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //Asocio el socket al endPoint 
                listener.Bind(localEndPoint);
                //Escucho hasta 10 request a la vez
                listener.Listen(10);

                //Espero la conexión de un cliente, y la acepto una vez efectuada
                Socket handler = listener.Accept();

                //Variables utilizadas en la serialización de los datos recibidos    
                string data = null;
                byte[] bytes = null;

                int desviador = 0;

                while (true)
                {
                    bytes = new byte[1024];
                    //Recibo el código del scanner
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    //A ser reemplazado por búsqueda de información en la BD
                    switch (data)
                    {
                        case "2112345678917":
                            desviador = 1;
                            break;

                        case "5984956745343":
                            desviador = 2;
                            break;

                        //case "7798156770283":
                        //    desviador = 2;
                        //    break;

                        //case "4005900170996":
                        //    desviador = 2;
                        //    break;

                        //case "7793928100749":
                        //    desviador = 2;
                        //    break;

                        default:
                            desviador = 0;
                            break;
                    }

                    if (desviador != 0)
                    {
                        //Activo el desviador en la variable MW3 del PLC
                        modbus.WriteSingleRegister(3, Convert.ToInt16(desviador));

                        //Seteo el pulso en M4 del PLC para indicar que se activará un desviador
                        modbus.WriteSingleCoil(4, true);                        
                    }

                    Console.WriteLine(data);

                    data = "";
                }

                //Console.WriteLine("Text received : {0}", data);

                //byte[] msg = Encoding.ASCII.GetBytes(data);
                //handler.Send(msg);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPresione alguna tecla para salir ");
            System.Environment.Exit(1);
        }

        public static void ObtenerConfig()
        {
            try
            {
                // Set up configuration sources.
                var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var configuration = builder.Build();

                serverIP = configuration["Server:ServerIP"];

                serverPort = configuration["Server:ServerPort"];

                plcIP = configuration["PLC:PLCIP"];

                plcPort = configuration["PLC:PLCPort"];

                //Console.WriteLine($"Server IP: {serverIP}");
                //Console.WriteLine($"Server Puerto: {serverPort}");

                //Console.WriteLine($"PLC IP: {plcIP}");
                //Console.WriteLine($"PLC Puerto: {plcPort}");

                if (String.IsNullOrEmpty(serverIP) || String.IsNullOrEmpty(serverIP) || String.IsNullOrEmpty(plcIP) || String.IsNullOrEmpty(plcPort))
                {
                    Console.WriteLine("\nError al obtener los datos de configuración");
                    Console.WriteLine("\nPresione alguna tecla para salir ");
                    System.Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error de configuración: " + ex.Message + " " + ex.InnerException);
                Console.WriteLine("\nPresione alguna tecla para salir ");
                System.Environment.Exit(1);
            }

        }
    }
}
