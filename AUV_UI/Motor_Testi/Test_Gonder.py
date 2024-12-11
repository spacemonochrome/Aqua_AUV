#!/usr/bin/env python
import time
import os
import threading
import serial
import TestMotor

delaysuresi = 10
delayy = delaysuresi/1000
dogrulamabyti = 95
TxData = [125,125,125,125,125,125,125,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti,dogrulamabyti]
TxEkBilgi = [65,65,65,65,65,65,65,65]
TxMotorverisi = [125,125,125,125,125,125,125]
MotorDeger = [125,125,125,125,125,125,125]
RxData = [0]
durmasarti = True
delayy = delaysuresi/1000

ser = serial.Serial(
        port='/dev/ttyS0',
        baudrate = 115200,
        parity=serial.PARITY_NONE,
        stopbits=serial.STOPBITS_ONE,
        bytesize=serial.EIGHTBITS,
        timeout=100
)

# 25 ve 225 değerleri arasında değer alır

def MotorValueUpdate():
        global durmasarti
        global MotorDeger

        MotorDeger = [125,125,125,125,125,125,125]
        time.sleep(1)

        MotorDeger = TestMotor.GelenDeger
        time.sleep(10)

        MotorDeger = [125,125,125,125,125,125,125]
        time.sleep(2)

        print("\n ---------- motorlar durdu ---------- \n")
        durmasarti = False

def MotorDegerUpdate():
        global durmasarti
        global MotorDeger
        global TxMotorverisi
        while durmasarti:
                for i in range(0,len(TxMotorverisi)):
                        if MotorDeger[i] > TxMotorverisi[i]:
                                TxMotorverisi[i] = TxMotorverisi[i] + 1
                        elif MotorDeger[i] == TxMotorverisi[i]:
                                pass
                        elif MotorDeger[i] < TxMotorverisi[i]:
                                TxMotorverisi[i] = TxMotorverisi[i] - 1
                time.sleep(delayy)

def UartWrite():
        global TxData
        global durmasarti
        global TxMotorverisi
        global TxEkBilgi
        global dogrulamabyti
        while durmasarti:
                TxData = TxMotorverisi + [dogrulamabyti] + TxEkBilgi
                ser.write(bytearray(TxData))
                time.sleep(0.1)

def UartRead():
        global RxData
        RxData = ser.read(16)
        if(RxData == "*"):
                os.system("sudo reboot")

def EkranaYaz():
        global TxData
        global RxData
        global durmasarti
        while durmasarti:
                time.sleep(0.1)
                os.system("clear")
                print("Giden Veri " + str(TxData) + " Gelen veri " + str(RxData))
                



if __name__ == '__main__':
        pid = os.getpid()
        print("%" + str(pid) + "%" )
        print("Motor Testi Basladi")

        t1 = threading.Thread(target = MotorValueUpdate)
        t2 = threading.Thread(target = MotorDegerUpdate)
        t3 = threading.Thread(target = UartWrite)
        t5 = threading.Thread(target = EkranaYaz)

        t1.start()
        t2.start()
        t3.start()
        t5.start()

        t1.join()
        t2.join()
        t3.join()
        t5.join()
        print("durdu")
        print("bitti")
