using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Quantum;

public static class DensityMatrixCalculator
{
    private const int ParallelizationThreshold = 14;
    
    public static Complex[,] Calculate(IReadOnlyDictionary<ulong, Complex> amplitudes, int totalWidth, int targetQubitOffset)
    {
        return totalWidth < ParallelizationThreshold 
            ? CalculateSequential(amplitudes, totalWidth, targetQubitOffset) 
            : CalculateParallel(amplitudes, totalWidth, targetQubitOffset);
    }
 
    /// <summary>
    ///     Calculates the reduced density matrix for a specific qubit within this (root) register.
    ///     This method performs the partial trace over all other qubits to derive the state of the target qubit.
    /// </summary>
    /// <param name="targetQubitOffsetInRoot">The offset of the target qubit within the root register.</param>
    /// <returns>A 2x2 complex matrix representing the reduced density matrix of the target qubit.</returns>
    private static Complex[,] CalculateSequential(IReadOnlyDictionary<ulong, Complex> amplitudes, int totalWidth, int targetQubitOffset)
    {
        // Initialize the components of the 2x2 density matrix.
        // rho10 is complex conjugate of 01 and implicitly clear
        Complex rho00 = Complex.Zero;
        Complex rho11 = Complex.Zero;
        Complex rho01 = Complex.Zero;

        // Create a mask to isolate the target qubit's bit.
        ulong targetMask = (ulong)1 << targetQubitOffset;

        // Create a mask for all bits in the root register, except the target qubit.
        // This is used to identify the 'rest of the system' state.
        ulong restOfSystemMask = ~targetMask & ((1UL << totalWidth) - 1UL);

        // Dictionaries to store amplitudes of the 'rest of the system' corresponding to the target qubit being 0 or 1.
        // These are used to calculate the off-diagonal elements (coherences).
        Dictionary<ulong, Complex> amplitudesOfRestWithQubit0 = new Dictionary<ulong, Complex>();
        Dictionary<ulong, Complex> amplitudesOfRestWithQubit1 = new Dictionary<ulong, Complex>();

        // diagonals (rho00 and rho11)
        // Iterate through all basis states and their amplitudes in the full system state vector.
        foreach (var entry in amplitudes)
        {
            ulong fullState = entry.Key;
            Complex amplitude = entry.Value;

            // Determine if the target qubit is 0 or 1 in the current full state.
            bool targetQubitIsOne = (fullState & targetMask) != 0;

            // Extract the state of the 'rest of the system' by masking out the target qubit's bit.
            ulong restState = fullState & restOfSystemMask;

            if (targetQubitIsOne)
            {
                // add probability contribution to rho11.
                rho11 += Complex.Pow(amplitude.Magnitude, 2);

                // Store the amplitude associated with this 'rest of system' state and target qubit 1.
                amplitudesOfRestWithQubit1[restState] = amplitude;
            }
            else
            {
                // add probability contribution to rho00.
                rho00 += Complex.Pow(amplitude.Magnitude, 2);

                // Store the amplitude associated with this 'rest of system' state and target qubit 0.
                amplitudesOfRestWithQubit0[restState] = amplitude;
            }
        }

        // Calculate the off-diagonal elements (rho01).
        // This sums the products of amplitudes (alpha_0 * conj(alpha_1)) for all matching 'rest of system' states.
        foreach (var entry in amplitudesOfRestWithQubit0)
        {
            ulong restState = entry.Key;
            Complex ampFor0 = entry.Value;

            // If a corresponding amplitude exists where the target qubit is 1 and the rest of the system is the same,
            // then contribute to the coherence.
            if (amplitudesOfRestWithQubit1.TryGetValue(restState, out Complex ampFor1))
                rho01 += ampFor0 * Complex.Conjugate(ampFor1);
        }

        return AssembleAndNormalizeDensityMatrix(rho00, rho11, rho01);
    }

    private static Complex[,] CalculateParallel(IReadOnlyDictionary<ulong, Complex> amplitudes, int totalWidth, int targetQubitOffset)
    {
        Complex rho00 = Complex.Zero;
        Complex rho11 = Complex.Zero;
        Complex rho01 = Complex.Zero;

        ulong targetMask = (ulong)1 << targetQubitOffset;
        ulong restOfSystemMask = ~targetMask & ((1UL << totalWidth) - 1UL);

        // Use ConcurrentDictionary for thread-safe writes.
        var amplitudesOfRestWithQubit0 = new ConcurrentDictionary<ulong, Complex>();
        var amplitudesOfRestWithQubit1 = new ConcurrentDictionary<ulong, Complex>();
        
        object _lock = new object();

        // Parallelize the first loop to calculate diagonal elements and populate dictionaries.
        // Use thread-local storage for the sums to avoid locking inside the loop.
        // Rach thread gets its own private tuple for summing.
        Parallel.ForEach(amplitudes, () => (localRho00: Complex.Zero, localRho11: Complex.Zero),
            (entry, loopState, localSums) =>
            {
                ulong fullState = entry.Key;
                Complex amplitude = entry.Value;
                bool targetQubitIsOne = (fullState & targetMask) != 0;
                ulong restState = fullState & restOfSystemMask;

                if (targetQubitIsOne)
                {
                    // Add to the thread's private sum
                    localSums.localRho11 += Complex.Pow(amplitude.Magnitude, 2);
                    amplitudesOfRestWithQubit1[restState] = amplitude;
                }
                else
                {
                    localSums.localRho00 += Complex.Pow(amplitude.Magnitude, 2);
                    amplitudesOfRestWithQubit0[restState] = amplitude;
                }

                return localSums; // Pass the updated local sums to the next iteration for this thread.
            },
            
            (localSums) =>
            {
                lock (_lock)
                {
                    rho00 += localSums.localRho00;
                    rho11 += localSums.localRho11;
                }
            }
        );

        
        Parallel.ForEach(
            amplitudesOfRestWithQubit0,
            () => Complex.Zero, // Thread-local sum for rho01
            (entry, loopState, localRho01) => 
            {
                ulong restState = entry.Key;
                Complex ampFor0 = entry.Value;
                if (amplitudesOfRestWithQubit1.TryGetValue(restState, out Complex ampFor1))
                    localRho01 += ampFor0 * Complex.Conjugate(ampFor1);
                
                return localRho01;
            },
            (localRho01) =>  
            {
                lock (_lock)
                {
                    rho01 += localRho01;
                }
            }
        );
 
        return AssembleAndNormalizeDensityMatrix(rho00, rho11, rho01);
    }


    private static Complex[,] AssembleAndNormalizeDensityMatrix(Complex rho00, Complex rho11, Complex rho01)
    {
        Complex[,] densityMatrix = new Complex[2, 2];
        densityMatrix[0, 0] = rho00;
        densityMatrix[1, 1] = rho11;
        densityMatrix[0, 1] = rho01;
        densityMatrix[1, 0] = Complex.Conjugate(rho01); // rho10 is the complex conjugate of rho01.

        // A density matrix must have a trace of 1. Normalize if necessary.
        double trace = densityMatrix[0, 0].Real + densityMatrix[1, 1].Real;
        if (Math.Abs(trace - 1.0) > QuantumComputer.Epsilon)
        {
            // Avoid division by zero if trace is somehow zero
            if (Math.Abs(trace) > QuantumComputer.Epsilon)
            {
                Complex invTrace = new Complex(1.0 / trace, 0);
                densityMatrix[0, 0] *= invTrace;
                densityMatrix[1, 1] *= invTrace;
                densityMatrix[0, 1] *= invTrace;
                densityMatrix[1, 0] *= invTrace;
            }
        }
        return densityMatrix;
    }   
}