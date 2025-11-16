using UnityEngine;

[System.Serializable]
public class QuestionData
{
    public string questionText;
    public string[] answers = new string[4]; // 0=A, 1=B, 2=C, 3=D
    
    // 0 untuk A, 1 untuk B, 2 untuk C, 3 untuk D
    public int correctAnswerIndex; 
}