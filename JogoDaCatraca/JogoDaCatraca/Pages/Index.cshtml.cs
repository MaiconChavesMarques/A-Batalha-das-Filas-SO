/*Autores: 2024 - Rodrigo de Freitas Lima – 12547510
Karl Cruz Altenhofen – 14585976
Maicon Chaves Marques - 14593530
Didrick Chancel Egnina Ndombi - 14822368
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace JogoDaCatraca.Pages
{
    public class IndexModel(ILogger<IndexModel> logger) : PageModel
    {
        // Semáforo para exclusão mútua
        private static readonly SemaphoreSlim mutex = new(1, 1);

        // Variáveis globais compartilhadas
        private static int fimDeJogo = 0; // 0: Em andamento, 1: Fila A venceu, 2: Fila B venceu
        private static int pessoasFilaA = 4; // Quantidade inicial na fila A
        private static int pessoasFilaB = 4; // Quantidade inicial na fila B
        private static readonly Random random = new();

        private readonly ILogger<IndexModel> _logger = logger;

        public void OnGet()
        {
            // Carrega a página
        }

        private static void TurmaAThread()
        {
            mutex.Wait(); // Exclusão mútua
            try
            {
                if (fimDeJogo == 0)
                {
                    pessoasFilaA--;
                    if (pessoasFilaA == 0)
                    {
                        fimDeJogo = 1; // Fila A venceu
                    }
                }
            }
            finally
            {
                mutex.Release(); // Libera a exclusão mútua
            }
        }

        private static void TurmaBThread()
        {
            mutex.Wait(); // Exclusão mútua
            try
            {
                if (fimDeJogo == 0)
                {
                    pessoasFilaB--;
                    if (pessoasFilaB == 0)
                    {
                        fimDeJogo = 2; // Fila B venceu
                    }
                }
            }
            finally
            {
                mutex.Release(); // Libera a exclusão mútua
            }
        }

        public JsonResult OnPostSubmit([FromBody] GameSubmission submission)
        {
            try
            {
                // Verifica se a jogada do usuário é válida e dentro do tempo limite
                int jogadaUsuario = -1;
                string feedback = $"Você demorou {submission.ElapsedTime:F2} segundos para responder.\n";

                if (submission.UserInput == submission.Senha && submission.ElapsedTime <= 10)
                {
                    jogadaUsuario = 1; // Jogada para Fila B
                    feedback += "Senha correta e tempo dentro do limite!";
                }
                else if (submission.UserInput == submission.Senha && submission.ElapsedTime > 10)
                {
                    jogadaUsuario = 0;
                    feedback += "Senha correta mas tempo excedido!";
                }
                else if (submission.UserInput != submission.Senha && submission.ElapsedTime <= 10)
                {
                    jogadaUsuario = 0;
                    feedback += "Senha incorreta e tempo dentro do limite!";
                }
                else
                {
                    jogadaUsuario = 0;
                    feedback += "Senha incorreta e tempo excedido!";
                }

                int jogadaMaquina = random.Next(0, 2);

                /*Após ambas as turmas (A e B) acertarem a senha, existe uma "corrida" para ver qual thread será a primeira a acessar o semáforo, ou seja, qual turma vai ter permissão para decrementar sua contagem primeiro.

                - Mesmo que ambas as turmas tenham acertado a senha, a ordem de execução das threads não é garantida. A thread que for capaz de "adquirir" o semáforo primeiro (de forma exclusiva) vai acessar o código crítico, enquanto a outra ficará esperando. 
                - Ou seja, após a validação da senha, ainda ocorre uma competição para ver qual thread consegue acessar o código primeiro. Não se sabe, com certeza, qual turma será a primeira a ter sua vez, já que isso depende do agendamento das threads pelo sistema operacional.

                No entanto, a exclusão mútua (usando o `mutex.Wait()` e `mutex.Release()`) é essencial para garantir que a alteração das variáveis globais (como `fimDeJogo`, `pessoasFilaA`, e `pessoasFilaB`) seja feita de maneira segura e sem interferências. Isso impede que as threads alterem simultaneamente essas variáveis, prevenindo inconsistências no estado do jogo.

                Portanto, mesmo que haja uma corrida para quem entra primeiro, a exclusão mútua garante que apenas uma thread possa modificar os dados compartilhados por vez, assegurando que o jogo seja processado corretamente, sem conflitos.
                */

                if (jogadaUsuario == 1 && jogadaMaquina == 1) // Ambos acertaram
                {
                    feedback += "\nAmbos conseguirão passar";
                    Thread turmaA = new(TurmaAThread);
                    Thread turmaB = new (TurmaBThread);
                    turmaA.Start();
                    turmaB.Start();
                    turmaB.Join();
                    turmaA.Join();
                }
                else if (jogadaUsuario == 0 && jogadaMaquina == 1) // Apenas a máquina acertou
                {
                    feedback += "\nSomente passará alguém da Turma A";
                    Thread turmaA = new (TurmaAThread);
                    turmaA.Start();
                    turmaA.Join();
                }
                else if (jogadaUsuario == 1 && jogadaMaquina == 0) // Apenas o jogador acertou
                {
                    feedback += "\nSomente passará alguém da Turma B";
                    Thread turmaB = new (TurmaBThread);
                    turmaB.Start();
                    turmaB.Join();
                }

                // Retorna o feedback e o progresso das filas
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
                _logger.LogError(ex, "Erro processando a submissão");
                return new JsonResult(new { success = false, error = "Ocorreu um erro ao enviar a submissão." });
            }
        }

        // Reinicia o jogo
        public JsonResult OnPostRestart()
        {
            try
            {
                /* A exclusão mútua é necessária para garantir que as variáveis sejam reiniciadas de forma segura.
                 * Sem o mutex, poderia ocorrer uma situação em que uma thread está lendo as variáveis enquanto outra está tentando reiniciá-las, causando inconsistências.
                 * Com a exclusão mútua, garantimos que apenas uma thread possa acessar as variáveis compartilhadas por vez, evitando conflitos.
                 */
                mutex.Wait(); // Exclusão mútua
                try
                {
                    fimDeJogo = 0;
                    pessoasFilaA = 4;
                    pessoasFilaB = 4;
                }
                finally
                {
                    mutex.Release(); // Libera a exclusão mútua
                }

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reiniciar o jogo");
                return new JsonResult(new { success = false, error = "Ocorreu um erro ao reiniciar o jogo." });
            }
        }
    }

    // Classe para representar a submissão do usuário
    public class GameSubmission
    {
        public required string UserInput { get; set; }
        public required string Senha { get; set; }
        public double ElapsedTime { get; set; }
    }
}