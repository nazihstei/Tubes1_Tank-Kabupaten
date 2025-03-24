using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text.Encodings.Web;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using Robocode.TankRoyale.Schema;

// ----------------- Main Class: LariAdaSule -----------------
public class LariAdaSule : Bot {

    // CONSTANT
    private double margin;
    // Attribute
    private EnemyBot ClosestBot;
    private EnemyBot TargetBot;

    // ----------------- Main to Start -----------------
    public static void Main(string[] args) {
        try {
            new LariAdaSule().Start();
        } catch (Exception ex) {
            Console.WriteLine($"Error di Main: {ex.Message}");
        }
    }
    public LariAdaSule() : base(BotInfo.FromFile("LariAdaSule.json")) {
        try {
            this.ClosestBot = null;
            this.TargetBot = null;
        } catch (Exception ex) {
            Console.WriteLine($"Error di Constructor: {ex.Message}");
        }
    }
    public override void OnGameStarted(GameStartedEvent e) {
        // Setup Color: chat gpt aja
        this.BodyColor  = Color.FromArgb(255, 34, 85, 34);     // Hijau hutan
        this.TurretColor= Color.FromArgb(255, 139, 69, 19);    // Cokelat kayu
        this.RadarColor = Color.FromArgb(255, 46, 139, 87);    // Hijau zamrud
        this.BulletColor= Color.FromArgb(255, 184, 134, 11);   // Emas tua
        this.ScanColor  = Color.FromArgb(255, 85, 107, 47);    // Hijau lumut
        this.GunColor   = Color.FromArgb(255, 160, 82, 45);    // Cokelat tanah
        this.TracksColor= Color.FromArgb(255, 47, 79, 79);     // Abu-abu kehijauan
    }


    // ----------------- Main Algorithm -----------------
    public override void Run() {
        try {
            Console.WriteLine("Welcome to Mobile Legend");
            this.AdjustRadarForGunTurn = true;
            this.AdjustRadarForBodyTurn = true;
            this.margin = Math.Min(this.ArenaWidth, this.ArenaHeight) / 3;
            while (this.IsRunning) {
                this.SetTurnRadarLeft(360);
                if (this.TargetBot != null) {
                    double firepower = this.CalculateFirepower();
                    this.SetGunPrecision(firepower);
                    this.SetFire(firepower);
                    this.TargetBot = null;
                }
                if (this.ClosestBot != null) {
                    this.AvoidClosestBot();
                }
                if (this.IsNearWall()) {
                    this.MoveTo(this.ArenaWidth/2, this.ArenaHeight/2);
                }
                this.Go();
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di Run: {ex.Message}");
        }
    }
    public void AvoidClosestBot() {
        try {
            if (this.ClosestBot.distance < this.margin) {
                double bearing = this.BearingTo(this.ClosestBot.X, this.ClosestBot.Y);
                this.SetTurnRight(bearing);
                this.SetForward(200);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error di AvoidClosestBot: {ex.Message}");
        }
    }


    // ----------------- Event Handler -----------------
    public override void OnScannedBot(ScannedBotEvent e) {
        try {
            Console.WriteLine($"I see a bot {e.ScannedBotId} at ({e.X}, {e.Y}).");
            
            if (this.ClosestBot == null) {
                this.ClosestBot = new EnemyBot(this, e);
            } else if (this.DistanceTo(e.X, e.Y) < this.DistanceTo(this.ClosestBot.X, this.ClosestBot.Y)) {
                this.ClosestBot = new EnemyBot(this, e);
            }
            if (this.TargetBot == null) {
                this.TargetBot = new EnemyBot(this, e);
            } else if (this.GunBearingTo(e.X, e.Y)<this.GunBearingTo(this.TargetBot.X, this.TargetBot.Y)) {
                this.TargetBot = new EnemyBot(this, e);
            }

            this.Rescan();
        } catch (Exception ex) {
            Console.WriteLine($"Error di OnScannedBot: {ex.Message}");
        }
    }
    public override void OnRoundStarted(RoundStartedEvent e) {
        try {
            this.ClosestBot = null;
            this.TargetBot = null;
        } catch (Exception ex) {
            Console.WriteLine($"Error di OnRoundStarted: {ex.Message}");
        }
    }


    // ----------------- Additional Method -----------------
    public double CalculateFirepower() {
        try {
            return Math.Max(0.1, 250/this.TargetBot.distance);
        } catch (Exception ex) {
            Console.WriteLine($"Error di CalculateFirepower: {ex.Message}");
            return -1;
        }
    }
    public void SetGunPrecision(double firepower) {
        try {
            // Hitung waktu tempuh peluru
            double bulletSpeed = this.CalcBulletSpeed(firepower);
            double distance = this.DistanceTo(this.TargetBot.X, this.TargetBot.Y);
            double timeToHit = distance / bulletSpeed;
            // Prediksi posisi musuh berdasarkan kecepatan dan arah geraknya
            double predictedX = this.TargetBot.X + Math.Cos(this.TargetBot.direction * Math.PI/180) * this.TargetBot.speed * timeToHit;
            double predictedY = this.TargetBot.Y + Math.Sin(this.TargetBot.direction * Math.PI/180) * this.TargetBot.speed * timeToHit;
            // Hitung bearing ke posisi yang diprediksi
            double predictedBearing = this.GunBearingTo(predictedX, predictedY);
            this.TurnGunLeft(predictedBearing*1.3);
        } catch (Exception ex) {
            Console.WriteLine($"Error di SetGunPrecision: {ex.Message}");
        } 
    }
    public bool IsNearWall() {
		double x = this.X;
		double y = this.Y;
        double WallMargin = this.margin;
		double arenaWidth = this.ArenaWidth;
		double arenaHeight = this.ArenaHeight;
		// Jika bot berada dalam jarak WallMargin dari batas arena, event dipicu
		return 	x <= WallMargin || x >= arenaWidth - WallMargin ||
			    y <= WallMargin || y >= arenaHeight - WallMargin;
	}
    public void MoveTo(double X, double Y) {
        try {
            this.SetTurnLeft(this.BearingTo(X, Y));
            this.SetForward(this.DistanceTo(X, Y));
        } catch (Exception ex) {
            Console.WriteLine($"Error di MoveTo: {ex.Message}");
        }
    }

}
