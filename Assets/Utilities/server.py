import UdpComms as U
import myrecorder
import time
import json
import pyaudio
import wave
import sys
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


game = U.UdpComms(udpIP="127.0.0.1", sendIP="127.0.0.1", portTX=8004, portRX=8005, enableRX=True, suppressWarnings=True)


while True:

    data2 = game.ReadReceivedData() # read data
    if data2 != None: # if NEW data has been received since last ReadReceivedData function call
        newQuery = data2
        messages.append({"role": "user", "content": newQuery})
        newResponse = GetGPTResponse(messages)
        messages.append({"role": "assistant", "content": newResponse})
        print(newResponse)
        game.SendData(newResponse)
        chat_transcript = ""
        for message in messages:
            if message["role"] != "system":
                chat_transcript += message["role"] + ": " + message["content"] + "\n\n"
