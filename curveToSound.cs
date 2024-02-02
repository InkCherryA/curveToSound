using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Media;
using System.Text;
using Rhino.Geometry.Intersect;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  private void RunScript(Curve waveForm, Curve envelope, Curve pitch, double frequency, double amplitude, bool play, ref object A)
  {
    // The duration of the sound in seconds
    int duration = 1;

    // Generate the wave
    byte[] wave = GenerateWave(waveForm, envelope, pitch, frequency, amplitude, duration);

    if (play) {
      PlayWave(wave);
    }
  }

  // <Custom additional code> 
  byte[] GenerateWave(Curve waveForm, Curve envelope, Curve pitch, double frequency, double amplitude, int duration) {
    using (MemoryStream memoryStream = new MemoryStream()) {
      using (BinaryWriter writer = new BinaryWriter(memoryStream, Encoding.UTF8)) {
        int sampleRate = 44100;
        short channels = 1;
        short bitsPerSample = 16;
        int sampleCount = sampleRate * duration;
        int samplesPerCycle = (int) (sampleRate / frequency);
        int dataSize = sampleCount * bitsPerSample / 8;
        int fileSize = 44 + dataSize; // Calculate the file size including the header

        // Write header
        writer.Write(Encoding.UTF8.GetBytes("RIFF"));
        writer.Write(fileSize);
        writer.Write(Encoding.UTF8.GetBytes("WAVEfmt "));
        writer.Write(16); // format chunk size
        writer.Write((short) 1); // audio format (1 = PCM)
        writer.Write(channels); // channels
        writer.Write(sampleRate); // sample rate
        writer.Write(sampleRate * bitsPerSample / 8);
        writer.Write((short) (channels * bitsPerSample / 8)); // block align
        writer.Write(bitsPerSample);

        writer.Write(Encoding.UTF8.GetBytes("data"));
        writer.Write(dataSize);

        Point3d oriWav = waveForm.PointAtStart;
        Point3d oriEnv = envelope.PointAtStart;
        // Point3d oriPit = pitch.PointAtStart;

        double amplitudeMax = amplitude * (Math.Pow(2, bitsPerSample - 1) - 1);

        for (int i = 0; i < sampleCount; i++) {

          // get Y of wave graph
          double xWav = oriWav.X + (i % samplesPerCycle) / (double) samplesPerCycle;
          Line lineWav = new Line(new Point3d(xWav, oriWav.Y - 1, 0), new Point3d(xWav, oriWav.Y + 1, 0));
          double valWav = Intersection.CurveLine(waveForm, lineWav, 0.01, 0.01)[0].PointA.Y - oriWav.Y;

          // get Y of envelope
          double xEnv = oriEnv.X + i / (double) sampleCount;
          Line lineEnv = new Line(new Point3d(xEnv, oriEnv.Y, 0), new Point3d(xEnv, oriEnv.Y + 1, 0));
          double valEnv = Intersection.CurveLine(envelope, lineEnv, 0.01, 0.01)[0].PointA.Y - oriEnv.Y;


          short sample = (short) (amplitudeMax * valWav * valEnv);

          writer.Write(sample);
        }

      }

      return memoryStream.ToArray();
    }
  }

  void PlayWave(byte[] wave) {
    using (MemoryStream memoryStream = new MemoryStream(wave)) {
      using (SoundPlayer player = new SoundPlayer(memoryStream)) {
        player.Play();
      }
    }
  }
  // </Custom additional code> 
}
