using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorNumeros.Models
{
    public class Player
    {
        public string Nombre { get; set; }

        public int Respuesta { get; set; }
        public int Tiempo { get; set; }

        public bool Resultado { get; set; }
    }
}

