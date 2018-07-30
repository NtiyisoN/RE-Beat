﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue  {

    public Sentence[] MainSentences;

    public Sentence[] RepeatSentences;

    public bool IsDialogueFinished = false;
}

[System.Serializable]
public class Sentence
{
    public string Name;

    [TextArea(3, 10)]
    public string DisplaySentence;

    public string firstAnswer;

    public string secondAnswer;

    public Sentence[] firstSentence;

    public Sentence[] secondSentence;
}