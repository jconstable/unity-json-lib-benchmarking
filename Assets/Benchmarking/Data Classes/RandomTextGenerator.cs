using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTextGenerator {
    private static string TextBlock = @"String theory Calabi yau formatted.svg Fundamental objects String Brane D-brane Perturbative theory Bosonic Superstring Type I Type II(IIA / IIB) Heterotic(SO(32) · E8×E8) Non-perturbative results S-duality T-duality M-theory AdS/CFT correspondence Phenomenology Phenomenology Cosmology Landscape Mathematics Mirror symmetry Monstrous moonshine Related concepts[show] 
Theorists[show] History Glossary vte In physics, string theory is a theoretical framework in which the point-like particles of particle physics are replaced by one-dimensional objects called strings.It describes how these strings propagate through space and interact with each other. On distance scales larger than the string scale, a string looks just like an ordinary particle, with its mass, charge, and other properties determined by the vibrational state of the string. In string theory, one of the many vibrational states of the string corresponds to the graviton, a quantum mechanical particle that carries gravitational force. Thus string theory is a theory of quantum gravity.
String theory is a broad and varied subject that attempts to address a number of deep questions of fundamental physics.String theory has been applied to a variety of problems in black hole physics, early universe cosmology, nuclear physics, and condensed matter physics, and it has stimulated a number of major developments in pure mathematics. Because string theory potentially provides a unified description of gravity and particle physics, it is a candidate for a theory of everything, a self-contained mathematical model that describes all fundamental forces and forms of matter. Despite much work on these problems, it is not known to what extent string theory describes the real world or how much freedom the theory allows in the choice of its details.
String theory was first studied in the late 1960s as a theory of the strong nuclear force, before being abandoned in favor of quantum chromodynamics. Subsequently, it was realized that the very properties that made string theory unsuitable as a theory of nuclear physics made it a promising candidate for a quantum theory of gravity.The earliest version of string theory, bosonic string theory, incorporated only the class of particles known as bosons.It later developed into superstring theory, which posits a connection called supersymmetry between bosons and the class of particles called fermions.Five consistent versions of superstring theory were developed before it was conjectured in the mid-1990s that they were all different limiting cases of a single theory in eleven dimensions known as M-theory.In late 1997, theorists discovered an important relationship called the AdS/CFT correspondence, which relates string theory to another type of physical theory called a quantum field theory.
One of the challenges of string theory is that the full theory does not have a satisfactory definition in all circumstances.Another issue is that the theory is thought to describe an enormous landscape of possible universes, and this has complicated efforts to develop theories of particle physics based on string theory. These issues have led some in the community to criticize these approaches to physics and question the value of continued research on string theory unification.";

    private static string m_NoNewlineTextBlock;

    private static string[] m_parts;

    public static string GetRandom()
    {
        if( string.IsNullOrEmpty( m_NoNewlineTextBlock ) )
        {
            m_NoNewlineTextBlock = TextBlock.Replace("\n", " ");
            m_parts = new string[1000];

            for( int i = 0; i < m_parts.Length; i++ )
            {
                int start = Mathf.Max(0, Random.Range(0, m_NoNewlineTextBlock.Length - 32));
                int end = Random.Range(1, m_NoNewlineTextBlock.Length - 1 - start);
                string s = m_NoNewlineTextBlock.Substring(start, end);
                m_parts[i] = s;

                Debug.Log(string.Format("{0} [{1},{2} {3}", i, start, end, s));
            }
        }
        
        return m_parts[Random.Range(0, m_parts.Length - 1)];
    }
}
