using Paralell.Comandos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Paralell
{
    public class Program
    {
        private static object locker = new object();
        private static int limite = 999;
        private static int processes = 0;
        private static AutoResetEvent exitEvent = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            List<string> lines = new List<string>();
            List<string> lineaActual = new List<string>();

            for(int i = 0; i < args.Length; i++)
            {
                if (args[i] == "/nuevo")
                {
                    AgregarLineaActual(lines, lineaActual);
                }
                else if (args[i] == "/l" || args[i] == "/limite")
                {
                    limite = int.Parse(args[++i]);
                }
                else if (args[i] == "/fin")
                {
                    AgregarLineaActual(lines, lineaActual);
                }
                else
                {
                    lineaActual.Add(args[i]);
                }
            }

            AgregarLineaActual(lines, lineaActual);

            lines.ForEach(c => Ejecutar(c));

            while (processes > 0)
            {
                exitEvent.WaitOne();
            }

            Console.WriteLine("Finished");
        }

        private static void Ejecutar(string c)
        {
            if (c.Contains(' '))
            {
                string cmd = c.Remove(c.IndexOf(' ')).ToLower();
                string args = c.Substring(c.IndexOf(' ') + 1).Trim();

                if(cmd == "for")
                {
                    ComandoFor(args);
                }
                else
                {
                    EjecutarProceso(cmd, args);
                }
            }
            else
            {
                EjecutarProceso(c, string.Empty);
            }
        }

        private static void EjecutarProceso(string comando)
        {
            EsperarLimite();

            lock (locker)
            {
                ++processes;
            }

            Console.WriteLine("Starting " + comando);

            Process p = new Process();

            p.EnableRaisingEvents = true;
            p.OutputDataReceived += (s, e) => Log(e.Data);
            p.ErrorDataReceived += (s, e) => Log(e.Data);
            p.Exited += (s, e) => ExitProcess(p);

            if(comando.Trim().StartsWith("$"))
            {
                string nombre = comando.Trim().Substring(1);
                if(nombre.Contains(" "))
                {
                    nombre = nombre.Remove(nombre.IndexOf(' '));
                }

                string carpeta = FindShell(nombre);
                if(carpeta != null)
                {
                    comando = Path.Combine(carpeta, comando.Substring(1));
                }
            }

            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = "/c " + comando;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }

        private static void EsperarLimite()
        {
            bool sobrepaso;

            do
            {
                lock (locker)
                {
                    sobrepaso = processes >= limite;
                }

                if(sobrepaso)
                {
                    exitEvent.Set();
                }
            }
            while (sobrepaso);

        }

        private static string FindShell(string file)
        {
            foreach (var dir in Environment.GetEnvironmentVariable("path").Split(';'))
            {
                var path = Path.Combine(dir, file);

                if (File.Exists(path) || File.Exists(path + ".bat") || File.Exists(path + ".exe") || File.Exists(path + ".cmd"))
                {
                    return dir;
                }
            }

            return null;
        }

        private static void EjecutarProceso(string nombre, string args)
        {
            EjecutarProceso(nombre + " " + args);
        }

        private static void ComandoFor(string args)
        {
            string[] arguments = args.Split(' ');
            ComandoFor comando = new ComandoFor();
            foreach(string cmd in comando.Procesar(arguments))
            {
                EjecutarProceso(cmd);
            }
        }

        private static void Log(string args)
        {
            Console.WriteLine(args);
        }

        private static void ExitProcess(Process p)
        {
            lock (locker)
            {
                --processes;
            }

            exitEvent.Set();

            Console.WriteLine("Process " + p.Id + " finished " + p.ExitCode);
        }

        private static void AgregarLineaActual(List<string> lineas, List<string> lineaActual)
        {
            if (lineaActual.Count > 0)
            {
                lineas.Add(string.Join(" ", lineaActual));
                lineaActual.Clear();
            }
        }
    }
}
