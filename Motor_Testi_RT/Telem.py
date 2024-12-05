import time
import os
import threading
import serial
import smbus
import subprocess

dogrulamabyti = 95
durmasarti = False
TxSpwm = [25,25,25,25]
TxMotorverisi = [125,125,125,125,125,125,125,125]
MotorDeger = [125,125,125,125,125,125,125,125]

TxData = [None]
RxData = [None]

telemetri = [None,None,None, None,None,None, None,None,None, None,None,None, None,None,None]
#Ax, Ay, Az, Gx, Gy, Gz, Px, Py, Pz, Cpi, Cstm, Cmpu


bus = smbus.SMBus(1)

ser = serial.Serial(
        port='/dev/ttyS0',
        baudrate = 115200,
        parity=serial.PARITY_NONE,
        stopbits=serial.STOPBITS_ONE,
        bytesize=serial.EIGHTBITS,
        timeout=100
)

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
                time.sleep(5/1000)

def TelemetriKaydet():
        global durmasarti
        while durmasarti:
                with open("telemetri_verisi.txt", "w") as dosya:
                        dosya.write(", ".join(map(str, telemetri)))

def sicaklik_oku_vcgencmd():
        global durmasarti, telemetri
        while durmasarti:
                try:
                        sonuc = subprocess.check_output(["vcgencmd", "measure_temp"]).decode("utf-8")
                        telemetri[12] = float(sonuc.split("=")[1].split("'")[0])
                except FileNotFoundError:
                        print("vcgencmd bulunamadi")

def ikarece():
        global durmasarti
        while durmasarti:
                pass

def EkranaYaz():
        global TxData
        global RxData
        global durmasarti
        while durmasarti:
                time.sleep(0.1)
                os.system("clear")
                print("\n Giden Veri " + str(TxData) + " \n " + "Gelen veri " + str(RxData) + " \n")

def UartWrite():
        global TxData
        global durmasarti
        while durmasarti:
                TxData = [dogrulamabyti] + TxMotorverisi + [dogrulamabyti] + TxSpwm + [dogrulamabyti]
                ser.write(bytearray(TxData))

def UartRead():
        global RxData
        global durmasarti
        while durmasarti:
                yeniData = ser.read(32)
                #doğruluk bitleri kontrol edilecek ondan sonra aktarma olucak
                # yeniData = RxData

def MotorValueUpdate():
        global durmasarti
        global MotorDeger

        MotorDeger = [125,125,125,125,125,125,125,125]
        time.sleep(1)

        while True:
                with open("TestMotorSayisalRT.txt", "r") as f:
                        satir = f.read().strip()
                        MotorDeger = [int(deger) for deger in satir.split(",")]

        MotorDeger = [125,125,125,125,125,125,125,125]
        time.sleep(1)

        print("\n ---------- motorlar durdu ---------- \n")
        durmasarti = False

if __name__ == '__main__':
        durmasarti = True
        time.sleep(3)
        t1 = threading.Thread(target = MotorValueUpdate)
        t2 = threading.Thread(target = MotorDegerUpdate)
        t3 = threading.Thread(target = EkranaYaz)
        t4 = threading.Thread(target = TelemetriKaydet)
        t5 = threading.Thread(target = sicaklik_oku_vcgencmd)

        t6 = threading.Thread(target = UartWrite)
        t7 = threading.Thread(target = UartRead)
        t8 = threading.Thread(target = ikarece)

        t1.start()
        t2.start()
        t3.start()
        t4.start()
        t5.start()
        t6.start()
        t7.start()
        t8.start()

        t1.join()
        t2.join()
        t3.join()
        t4.join()
        t5.join()
        t6.join()
        t7.join()
        t8.join()
        print("durdu")
