import gradio as gr
import openai
import time

openai.api_key="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"

messages = [{"role": "system", "content": "You are my best friend. Respond in 2 sentences, and be friendly. Ask for more information if needed."}]


def SpeechToText(audio):
    audio_file = open(audio, "rb")
    transcript = openai.Audio.transcribe("whisper-1", audio_file)

    return transcript["text"]

def GetGPTResponse(messages):
    response = openai.ChatCompletion.create(
        model="gpt-3.5-turbo",
        messages=messages
    )
    return response["choices"][0]["message"]["content"]


def Main(audio):
    global messages

    newQuery = SpeechToText(audio)
    messages.append({"role": "user", "content": newQuery})

    newResponse = GetGPTResponse(messages)
    messages.append({"role": "assistant", "content": newResponse})

    chat_transcript = ""
    for message in messages:
        if message["role"] != "system":
            chat_transcript += message["role"] + ": " + message["content"] + "\n\n"


    return chat_transcript
