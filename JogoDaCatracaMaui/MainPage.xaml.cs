using System.Diagnostics;
using System.Threading;

namespace JogoDaCatracaMaui;

public partial class MainPage : ContentPage
{
    static readonly SemaphoreSlim mutex = new(1, 1);
    static int fimDeJogo = 0; // 0: Em andamento, 1: Fila A venceu, 2: Fila B venceu
    static int pessoasFilaA = 4;
    static int pessoasFilaB = 4;
    static readonly Random random = new();
    readonly Stopwatch stopwatch = new();

    public MainPage()
    {
        InitializeComponent();
        GenerateSenha();
    }

    // Método chamado quando o botão de iniciar jogo é clicado
    private void OnStartGame(object sender, EventArgs e)
    {
        // Quando clicado, esconde botão de iniciar jogo e mostra o jogo
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StartGameButton.IsVisible = false;
            GameInterface.IsVisible = true;
            GenerateSenha();
        });
    }

    // Método para gerar uma senha aleatória
    private void GenerateSenha()
    {
        string senha = new(new char[8].Select(c => (char)random.Next(97, 123)).ToArray());
        SenhaInput.Text = senha;
        UserInput.Text = string.Empty;
        stopwatch.Restart();
    }

    // Método chamado quando o botão de reiniciar é clicado
    private void OnRestart(object sender, EventArgs e)
    {
        // Reseta as variáveis do jogo
        fimDeJogo = 0;
        pessoasFilaA = 4;
        pessoasFilaB = 4;

        // Reseta
        MainThread.BeginInvokeOnMainThread(() =>
        {
            TurmaAProgressBar.Progress = 1;
            TurmaBProgressBar.Progress = 1;
            FeedbackLabel.Text = string.Empty;
            GameStatusLabel.Text = string.Empty;
            GenerateSenha();
            UserInput.Text = string.Empty;
        });
    }

    // Método chamado quando o botão de enviar é clicado
    private async void OnSubmit(object sender, EventArgs e)
    {
        if (fimDeJogo != 0) // Previne que o jogo continue após o fim
        {
            return;
        }

        stopwatch.Stop();

        string input = UserInput.Text;
        string senha = SenhaInput.Text;
        TimeSpan elapsed = stopwatch.Elapsed;

        FeedbackLabel.Text = $"Você demorou {elapsed.TotalSeconds:F2} segundos para responder.";

        int jogadaUsuario = -1;

        if (input == senha && elapsed.TotalSeconds <= 10)
        {
            // Se o usuario acertou a senha e respondeu em menos de 10 segundos
            jogadaUsuario = 1;
            FeedbackLabel.Text += "\nSenha correta e tempo dentro do limite!";
        }
        else
        {
            // Se o usuario errou a senha ou respondeu em mais de 10 segundos
            jogadaUsuario = 0;
            FeedbackLabel.Text += "\nSenha incorreta ou tempo excedido.";
        }

        int jogadaMaquina = random.Next(0, 2);

        // Verifica quem ganhou a rodada
        if (jogadaUsuario == 1 && jogadaMaquina == 1)
        {
            FeedbackLabel.Text += "\nAmbos conseguirão passar.";
            await RunThreadsAsync(true, true);
        }
        else if (jogadaUsuario == 0 && jogadaMaquina == 1)
        {
            FeedbackLabel.Text += "\nSomente passará alguém da Turma A.";
            await RunThreadsAsync(true, false);
        }
        else if (jogadaUsuario == 1 && jogadaMaquina == 0)
        {
            FeedbackLabel.Text += "\nSomente passará alguém da Turma B.";
            await RunThreadsAsync(false, true);
        }

        // Se o jogo acabou, mostra o vencedor
        if (fimDeJogo != 0)
        {
            GameStatusLabel.Text = $"Fim de jogo! Vencedor: {(fimDeJogo == 1 ? "Fila A" : "Fila B")}";
        }
        else
        {
            GenerateSenha();
            UserInput.Text = string.Empty;
        }
    }

    private async Task RunThreadsAsync(bool turmaA, bool turmaB)
    {
        if (turmaA)
            await Task.Run(() => TurmaAThread());
        if (turmaB)
            await Task.Run(() => TurmaBThread());
    }

    // Método para decrementar o contador da Turma A
    private void TurmaAThread()
    {
        mutex.Wait(); // Semáforo para operação thread-safe
        if (fimDeJogo == 0)
        {
            pessoasFilaA--; // Decrementa o contador da Turma A
            UpdateProgressBar(pessoasFilaA / 4.0, 'A');
            if (pessoasFilaA == 0)
            {
                fimDeJogo = 1; // Se a Turma A chegar a zero, marca o fim de jogo
            }
        }
        mutex.Release(); // Libera o semáforo
    }

    // Método para decrementar o contador da Turma B
    private void TurmaBThread()
    {
        mutex.Wait(); // Semáforo para operação thread-safe
        if (fimDeJogo == 0)
        {
            pessoasFilaB--; // Decrementa o contador da Turma B
            UpdateProgressBar(pessoasFilaB / 4.0, 'B');
            if (pessoasFilaB == 0)
            {
                fimDeJogo = 2; // Se a Turma B chegar a zero, marca o fim de jogo
            }
        }
        mutex.Release(); // Libera o semáforo
    }

    // Método para atualizar a barra de progresso na UI
    private void UpdateProgressBar(double progress, char turma)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (turma == 'A')
            {
                TurmaAProgressBar.Progress = progress;
            }
            else if (turma == 'B')
            {
                TurmaBProgressBar.Progress = progress;
            }
        });
    }
}
