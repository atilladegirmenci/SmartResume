using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartResume.Services.Interfaces
{
    public interface IOcrService
    {
        public string ExtractTextFromPdf(byte[] data);
    }
}
