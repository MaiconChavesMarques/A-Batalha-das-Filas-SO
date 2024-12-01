/*Autores: 2024 - Rodrigo de Freitas Lima – 12547510
Karl Cruz Altenhofen – 14585976
Maicon Chaves Marques - 14593530
Didrick Chancel Egnina Ndombi - 14822368
*/

using System;
using System.Threading;
using System.Diagnostics;

class Program{
    // Semáforo para exclusão mútua
    static SemaphoreSlim mutex = new SemaphoreSlim(1, 1);

    // Variáveis globais compartilhadas
    static int fimDeJogo = 0; // 0: Em andamento, 1: Fila A venceu, 2: Fila B venceu
    static int pessoasFilaA = 4; // Quantidade inicial na fila A
    static int pessoasFilaB = 4; // Quantidade inicial na fila B
    static Random random = new Random();

    /*
    Comentário sobre o problema sem exclusão mútua:
    SEM MUTEX:
    1. Turma A entra no bloco crítico primeiro e decrementa `pessoasFilaA` para 0.
    - Condição: `if (pessoasFilaA == 0)` é verdadeira.
    - Antes de alterar `fimDeJogo` para 1, o controle é trocado para Turma B.
    2. Turma B entra no bloco crítico com `fimDeJogo == 0` e decrementa `pessoasFilaB` para 0.
    - Como `fimDeJogo` ainda não foi alterado por A, B considera que o jogo está em andamento.
    3. Controle retorna para Turma A, que altera `fimDeJogo` para 1.
    4. Controle retorna para Turma B - Turma B altera `fimDeJogo` para 2.
    5. Resultado: Ambas as turmas atualizam `fimDeJogo`, mas como B foi a última a alterar, é declarado vencedora.
    - Problema: A chegou a 0 primeiro, mas devido à falta de exclusão mútua, B conseguiu executar simultaneamente.

    Solução:
    - O uso de `mutex.Wait()` e `mutex.Release()` garante que somente uma turma pode acessar as variáveis globais compartilhadas por vez.
    - Isso impede que duas threads decremetem as filas ou alterem `fimDeJogo` ao mesmo tempo.
    */

    static void TurmaAThread(){
        mutex.Wait(); // Exclusão mútua
        if (fimDeJogo == 0){
            pessoasFilaA--;
            if (pessoasFilaA == 0){
                fimDeJogo = 1; // Fila A venceu
            }
        }
        Console.WriteLine("Alguém da Turma A passou");
        mutex.Release(); // Libera a exclusão mútua
    }

    static void TurmaBThread(){
        mutex.Wait(); // Exclusão mútua
        if (fimDeJogo == 0){
            pessoasFilaB--;
            if (pessoasFilaB == 0){
                fimDeJogo = 2; // Fila B venceu
            }
        }
        Console.WriteLine("Alguém da Turma B passou");
        mutex.Release(); // Libera a exclusão mútua
    }

    static void Main(string[] args){
        Console.WriteLine("Bem-vindo ao jogo da catraca!");
        Console.WriteLine("Regras:");
        Console.WriteLine("1. A primeira fila que chegar a 0 vence.");
        Console.WriteLine("2. Você representa a fila B.");

        Stopwatch stopwatch = new Stopwatch();

        // Loop do jogo
        while (true){
            // Geração da senha
            string senha = new string(new char[8].Select(c => (char)random.Next(97, 123)).ToArray());

            Console.Write("Digite a senha o mais rápido que conseguir:\n" + senha + "\n");
            stopwatch.Restart();
            string input = Console.ReadLine();
            stopwatch.Stop();

            TimeSpan elapsed = stopwatch.Elapsed;
            Console.WriteLine($"Você demorou {elapsed.TotalSeconds:F2} segundos para responder.");

            int jogadaUsuario = -1;

            if (input == senha && elapsed.TotalSeconds <= 10){
                jogadaUsuario = 1; // Jogada para Fila B
                Console.WriteLine("Senha correta e tempo dentro do limite!" + "\n");
            }else if(input == senha && elapsed.TotalSeconds > 10){ 
                jogadaUsuario = 0; 
                Console.WriteLine("Senha correta mas tempo excedido!" + "\n");
            } else if(input != senha && elapsed.TotalSeconds <= 10){
                jogadaUsuario = 0;
                Console.WriteLine("Senha incorreta e tempo dentro do limite!" + "\n");
            }else{
                jogadaUsuario = 0;
                Console.WriteLine("Senha incorreta e tempo excedido!" + "\n");
            }

            int jogadaMaquina = random.Next(0, 2);

            /*Após ambas as turmas (A e B) acertarem a senha, existe uma "corrida" para ver qual thread será a primeira a acessar o semáforo, ou seja, qual turma vai ter permissão para decrementar sua contagem primeiro.

            - Mesmo que ambas as turmas tenham acertado a senha, a ordem de execução das threads não é garantida. A thread que for capaz de "adquirir" o semáforo primeiro (de forma exclusiva) vai acessar o código crítico, enquanto a outra ficará esperando. 
            - Ou seja, após a validação da senha, ainda ocorre uma competição para ver qual thread consegue acessar o código primeiro. Não se sabe, com certeza, qual turma será a primeira a ter sua vez, já que isso depende do agendamento das threads pelo sistema operacional.

            No entanto, a exclusão mútua (usando o `mutex.Wait()` e `mutex.Release()`) é essencial para garantir que a alteração das variáveis globais (como `fimDeJogo`, `pessoasFilaA`, e `pessoasFilaB`) seja feita de maneira segura e sem interferências. Isso impede que as threads alterem simultaneamente essas variáveis, prevenindo inconsistências no estado do jogo.

            Portanto, mesmo que haja uma corrida para quem entra primeiro, a exclusão mútua garante que apenas uma thread possa modificar os dados compartilhados por vez, assegurando que o jogo seja processado corretamente, sem conflitos.
            */

            if (jogadaUsuario == 1 && jogadaMaquina == 1){
                Console.WriteLine("Ambos conseguirão passar");
                Thread turmaA = new Thread(TurmaAThread);
                Thread turmaB = new Thread(TurmaBThread);
                turmaA.Start();
                turmaB.Start();
                turmaB.Join();
                turmaA.Join();
                Console.WriteLine("");
            } else if (jogadaUsuario == 0 && jogadaMaquina == 1){
                Console.WriteLine("Somente passará alguém da Turma A");
                Thread turmaA = new Thread(TurmaAThread);
                turmaA.Start();
                turmaA.Join();
                Console.WriteLine("");
            } else if (jogadaUsuario == 1 && jogadaMaquina == 0){
                Console.WriteLine("Somente passará alguém da Turma B");
                Thread turmaB = new Thread(TurmaBThread);
                turmaB.Start();
                turmaB.Join();
                Console.WriteLine("");
            }

            // Verifica o fim do jogo
            if (fimDeJogo != 0){
                Console.WriteLine($"Fim de jogo! Vencedor: {(fimDeJogo == 1 ? "Fila A" : "Fila B")}");
                break;
            }
        }
    }
}
