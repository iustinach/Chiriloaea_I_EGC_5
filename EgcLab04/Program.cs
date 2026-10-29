//Chiriloaea Iustina
//3133A
using Egc;

// Program.cs conține DOAR inițializarea aplicației:
// creăm fereastra (clasa Game) și pornim bucla principală.
Console.WriteLine("========== EGC LAB 04 - CONTROALE ==========");
Console.WriteLine();
Console.WriteLine("SELECTARE OBIECT");
Console.WriteLine("  TAB             - Selecteaza urmatorul obiect");
Console.WriteLine();
Console.WriteLine("MISCARE OBIECT");
Console.WriteLine("  SAGETI           - Muta obiectul selectat");
Console.WriteLine();
Console.WriteLine("ROTATIE SI SCALARE");
Console.WriteLine("  Q / E            - Rotire");
Console.WriteLine("  Z / X            - Micsorare / Marire");
Console.WriteLine("  LEFT SHIFT       - Viteza de 3 ori mai mare");
Console.WriteLine();
Console.WriteLine("CAMERA");
Console.WriteLine("  W / A / S / D    - Deplasare camera");
Console.WriteLine("  ROTITA MOUSE     - Zoom");
Console.WriteLine("  O                - Schimba perspectiva");
Console.WriteLine("  F                - Afiseaza doar conturul");
Console.WriteLine("  R                - Reseteaza scena");
Console.WriteLine();
Console.WriteLine("============================================");
Console.WriteLine();
using var game = new Game(800, 600, "EGC – Laborator 04");
game.Run();
