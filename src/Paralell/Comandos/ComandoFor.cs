using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paralell.Comandos
{
    public class ComandoFor
    {
        public ComandoFor()
        {
        }

        /*public string Variable { get; set; }

        public string Busqueda { get; set; }

        public string Accion { get; set; }

        public bool Recursivo { get; set; }*/

        public IEnumerable<string> Procesar(string[] arguments)
        {
            bool recursivo = false;
            string variable = "";
            string busqueda = "";
            string accion = "";

            for (int i = 0; i < arguments.Length; i++)
            {
                string arg = arguments[i].Trim();

                if (arg.ToLower() == "/r")
                {
                    recursivo = true;
                }
                else if (arg.Contains("%"))
                {
                    variable = arg;
                }
                else if (arg.ToLower() == "in")
                {
                    busqueda = arguments[++i].Trim('(', ')', ' ');
                }
                else if (arg.ToLower() == "do")
                {
                    accion = string.Join(" ", arguments.Skip(i + 1));
                    break;
                }
            }

            bool relativo = false;

            if (!busqueda.Contains("\\"))
            {
                busqueda = Path.Combine(Environment.CurrentDirectory, busqueda);
                relativo = true;
            }

            DirectoryInfo dir = new DirectoryInfo(busqueda.Remove(busqueda.LastIndexOf("\\")));
            foreach (var file in dir.GetFiles(busqueda.Substring(busqueda.LastIndexOf("\\") + 1), recursivo ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                string localPath = relativo ? file.FullName.Replace(dir.FullName, ".\\") : file.FullName;
                yield return accion.Replace(variable, localPath);
            }
        }
    }
}
