using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartResume.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> AnalyzeResumeAsync(string rawResumeText);
    }
}
