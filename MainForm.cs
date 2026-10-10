using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace LoLBR_OfflineChat;

public class MainForm : Form
{
    private readonly string hostsPath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "System32",
            "drivers",
            "etc",
            "hosts");

    private readonly string[] chatDomains =
    {
        "br.chat.si.riotgames.com",
        "sa1.chat.si.riotgames.com",
        "sa2.chat.si.riotgames.com",
        "la1.chat.si.riotgames.com",
        "la2.chat.si.riotgames.com",
        "na1.chat.si.riotgames.com",
        "na2.chat.si.riotgames.com"
    };

    // É us guri né

    private Label statusLabel = null!;

    public MainForm()
    {
        InitializeInterface();
        UpdateStatus();
    }

    private void InitializeInterface()
    {
        Text = "LoLBR Offline Chat";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(520, 420);
        MinimumSize = new Size(520, 420);
        BackColor = Color.FromArgb(18, 18, 24);
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        var title = new Label
        {
            Text = "LOLBR OFFLINE CHAT",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 170, 80),
            AutoSize = true,
            Location = new Point(135, 35)
        };

        var subtitle = new Label
        {
            Text = "Controle a conexão do chat do League of Legends",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(105, 75)
        };

        statusLabel = new Label
        {
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(440, 50),
            Location = new Point(40, 115)
        };

        var invisibleButton = CreateButton(
            "FICAR INVISÍVEL",
            Color.FromArgb(125, 85, 180));

        invisibleButton.Location = new Point(40, 190);
        invisibleButton.Click += (_, _) => EnableOfflineMode();

        var onlineButton = CreateButton(
            "FICAR DISPONÍVEL",
            Color.FromArgb(35, 145, 95));

        onlineButton.Location = new Point(270, 190);
        onlineButton.Click += (_, _) => DisableOfflineMode();

        var closeButton = CreateButton(
            "FECHAR CLIENTE DA RIOT",
            Color.FromArgb(150, 65, 65));

        closeButton.Location = new Point(40, 255);
        closeButton.Size = new Size(440, 45);
        closeButton.Click += (_, _) => CloseRiotClient();

        var footer = new Label
        {
            Text = "O programa modifica o arquivo hosts do Windows.",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(125, 330)
        };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(statusLabel);
        Controls.Add(invisibleButton);
        Controls.Add(onlineButton);
        Controls.Add(closeButton);
        Controls.Add(footer);
    }

    private Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(210, 45),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;

        return button;
    }

    private bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(
            WindowsBuiltInRole.Administrator);
    }

    private void EnableOfflineMode()
    {
        try
        {
            if (!IsAdministrator())
            {
                MessageBox.Show(
                    "Execute o programa como Administrador.",
                    "Permissão necessária",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var lines = File.Exists(hostsPath)
                ? File.ReadAllLines(hostsPath, Encoding.UTF8)
                : Array.Empty<string>();

            var filtered = lines
                .Where(line =>
                    !chatDomains.Any(domain =>
                        line.Contains(domain,
                            StringComparison.OrdinalIgnoreCase)))
                .Where(line =>
                    !line.Contains(
                        "# LoL Chat Offline",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            filtered.Add("");
            filtered.Add("# LoL Chat Offline - inicio");

            foreach (var domain in chatDomains)
            {
                filtered.Add($"0.0.0.0 {domain}");
            }

            filtered.Add("# LoL Chat Offline - fim");

            File.WriteAllLines(
                hostsPath,
                filtered,
                new UTF8Encoding(false));

            FlushDns();

            UpdateStatus();

            var result = MessageBox.Show(
                "O bloqueio do chat foi ativado.\n\n" +
                "Deseja fechar o cliente da Riot agora?",
                "Modo invisível ativado",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                CloseRiotClient();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível modificar o arquivo hosts.\n\n" +
                ex.Message,
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void DisableOfflineMode()
    {
        try
        {
            if (!IsAdministrator())
            {
                MessageBox.Show(
                    "Execute o programa como Administrador.",
                    "Permissão necessária",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (!File.Exists(hostsPath))
                return;

            var lines = File.ReadAllLines(
                hostsPath,
                Encoding.UTF8);

            var filtered = lines
                .Where(line =>
                    !chatDomains.Any(domain =>
                        line.Contains(domain,
                            StringComparison.OrdinalIgnoreCase)))
                .Where(line =>
                    !line.Contains(
                        "# LoL Chat Offline",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            File.WriteAllLines(
                hostsPath,
                filtered,
                new UTF8Encoding(false));

            FlushDns();

            UpdateStatus();

            MessageBox.Show(
                "O bloqueio foi removido.\n\n" +
                "Reinicie o cliente da Riot para o chat voltar a conectar.",
                "Modo disponível",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível restaurar o arquivo hosts.\n\n" +
                ex.Message,
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private bool IsOfflineModeEnabled()
    {
        if (!File.Exists(hostsPath))
            return false;

        var content = File.ReadAllText(
            hostsPath,
            Encoding.UTF8);

        return chatDomains.Any(domain =>
            content.Contains(
                domain,
                StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateStatus()
    {
        if (statusLabel == null)
            return;

        if (IsOfflineModeEnabled())
        {
            statusLabel.Text = "● MODO INVISÍVEL ATIVO";
            statusLabel.ForeColor = Color.FromArgb(190, 120, 255);
        }
        else
        {
            statusLabel.Text = "● CHAT DISPONÍVEL";
            statusLabel.ForeColor = Color.FromArgb(70, 210, 130);
        }
    }

    private void FlushDns()
    {
        try
        {
            using var process = new Process();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ipconfig.exe",
                Arguments = "/flushdns",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            process.Start();
            process.WaitForExit();
        }
        catch
        {
            // O bloqueio do hosts continua funcionando mesmo se a limpeza do DNS falhar
        }
    }

    private void CloseRiotClient()
    {
        string[] processes =
        {
            "LeagueClientUx",
            "LeagueClient",
            "RiotClientUx",
            "RiotClientServices"
        };

        foreach (var processName in processes)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                    catch
                    {
                        // Processo pode já ter sido encerrado
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                // Ignora processos que não existam
            }
        }

        MessageBox.Show(
            "Os processos do cliente da Riot foram encerrados.",
            "Cliente fechado",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}