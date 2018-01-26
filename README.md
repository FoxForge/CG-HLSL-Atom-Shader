# CG-HLSL-Atom-Shader
A randomizing atom shader in CG with values exposed to change pattern types.

## How To Use

When combined with the c# class in the Unity Game Engine, the developer can use the keyboard buttons to change
the shader at runtime. The class also has the ability to stress test your GPU by increasing the amount of "electrons"
the atom produces for the shader. Running on a GTX 1070, I hit less than 30 frames when inceasing the electron count
to roughly 40,000.

Or simply if you want to see the shader code then it is here to view.

When running in the Unity Game Engine default behaivour is to randomize the shader values every 20 seconds.

- Space Bar: Will randomize all values on the shader instantly

- Return: Will toggle lock the values and prevent them from being changed. (so you can sit back and admire its magic!)

Feel free to use the shader and c# code provided however you like. 
It would be nice to be referenced in any work but is not essential.
