using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace JogoDaCatraca.Pages
{
    public class IndexModel : PageModel
    {
        private static readonly SemaphoreSlim mutex = new(1, 1);
        private static int fimDeJogo = 0; // 0: Em andamento, 1: Fila A venceu, 2: Fila B venceu
        private static int pessoasFilaA = 4;
        private static int pessoasFilaB = 4;
        private static readonly Random random = new();

        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Initial page load
        }

        public async Task<JsonResult> OnPostSubmitAsync([FromBody] GameSubmission submission)
        {
            try
            {
                bool jogadaUsuario = submission.UserInput == submission.Senha && submission.ElapsedTime <= 10;
                bool jogadaMaquina = random.Next(0, 2) == 1;

                string feedback = $"Você demorou {submission.ElapsedTime:F2} segundos para responder.\n";
                feedback += jogadaUsuario ? "Senha correta e tempo dentro do limite!" : "Senha incorreta ou tempo excedido.";

                if (jogadaUsuario && jogadaMaquina)
                {
                    feedback += "\nAmbos conseguirão passar.";
                    await ProcessMoves(true, true);
                }
                else if (!jogadaUsuario && jogadaMaquina)
                {
                    feedback += "\nSomente passará alguém da Turma A.";
                    await ProcessMoves(true, false);
                }
                else if (jogadaUsuario && !jogadaMaquina)
                {
                    feedback += "\nSomente passará alguém da Turma B.";
                    await ProcessMoves(false, true);
                }

                var response = new
                {
                    success = true,
                    feedback,
                    turmaAProgress = pessoasFilaA / 4.0,
                    turmaBProgress = pessoasFilaB / 4.0,
                    gameEnded = fimDeJogo != 0,
                    gameStatus = fimDeJogo != 0 ? $"Fim de jogo! Vencedor: Fila {(fimDeJogo == 1 ? "A" : "B")}" : ""
                };

                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game submission");
                return new JsonResult(new { success = false, error = "An error occurred processing your submission." });
            }
        }

        public JsonResult OnPostRestart()
        {
            try
            {
                fimDeJogo = 0;
                pessoasFilaA = 4;
                pessoasFilaB = 4;

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting game");
                return new JsonResult(new { success = false, error = "An error occurred restarting the game." });
            }
        }

        private async Task ProcessMoves(bool turmaA, bool turmaB)
        {
            await Task.Run(() =>
            {
                if (turmaA)
                    ProcessTurmaA();
                if (turmaB)
                    ProcessTurmaB();
            });
        }

        private void ProcessTurmaA()
        {
            mutex.Wait();
            try
            {
                if (fimDeJogo == 0)
                {
                    pessoasFilaA--;
                    if (pessoasFilaA == 0)
                        fimDeJogo = 1;
                }
            }
            finally
            {
                mutex.Release();
            }
        }

        private void ProcessTurmaB()
        {
            mutex.Wait();
            try
            {
                if (fimDeJogo == 0)
                {
                    pessoasFilaB--;
                    if (pessoasFilaB == 0)
                        fimDeJogo = 2;
                }
            }
            finally
            {
                mutex.Release();
            }
        }
    }

    public class GameSubmission
    {
        public string UserInput { get; set; }
        public string Senha { get; set; }
        public double ElapsedTime { get; set; }
    }
}