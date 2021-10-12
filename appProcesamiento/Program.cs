using EasyModbus;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace appProcesamiento
{
    class Program
    {
        static void Main(string[] args)
        {

            //Inicializo el server donde escucho al cliente scanner
            IPHostEntry host = Dns.GetHostEntry("10.200.13.155");           //USAR IP DE LA COMPUTADORA
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);    //USAR PUERTO DEL SCANNER

            //Inicializo modbus para comunicación con PLC
            ModbusClient modbus = new ModbusClient();
            modbus.IPAddress = "10.20.65.195";                              //USAR IP FIJA DEL PLC
            modbus.Port = Convert.ToInt32(502);                             //USAR PUERTO DEL PLC

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

            try
            {
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

                byte[] msg = Encoding.ASCII.GetBytes(data);
                handler.Send(msg);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue...");
            Console.ReadKey();
        
        }
    }
}
