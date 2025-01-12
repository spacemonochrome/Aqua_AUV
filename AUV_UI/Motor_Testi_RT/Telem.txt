import time
import os
import threading
import serial
import smbus
import subprocess

MPU9250_ADDRESS = 0x68

# MPU9250 register adresleri
PWR_MGMT_1 = 0x6B
ACCEL_XOUT_H = 0x3B
GYRO_XOUT_H = 0x43
ACCEL_CONFIG = 0x1C
GYRO_CONFIG = 0x1B
TEMP_OUT_H = 0x41

bus = smbus.SMBus(1)

dogrulamabyti = 95
durmasarti = False
TxSpwm = [125,125,125,125]
Spwm = [125,125,125,125]
TxMotorverisi = [125,125,125,125,125,125,125,125]
MotorDeger = [125,125,125,125,125,125,125,125]
TxData = [None]
RxData = [None]
telemetri = [None,None,None, None,None,None, None,None,None, None,None,None]
#Ax, Ay, Az, Gx, Gy, Gz, Px, Py, Pz, Cpi, Cstm, Cmpu

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
        global TxSpwm
        global Spwm
        while durmasarti:
                for i in range(0,len(MotorDeger)):
                        if MotorDeger[i] > TxMotorverisi[i]:
                                TxMotorverisi[i] = TxMotorverisi[i] + 1
                        elif MotorDeger[i] == TxMotorverisi[i]:
                                pass
                        elif MotorDeger[i] < TxMotorverisi[i]:
                                TxMotorverisi[i] = TxMotorverisi[i] - 1
                for i in range(0,len(Spwm)):
                        if Spwm[i] > TxSpwm[i]:
                                TxSpwm[i] = TxSpwm[i] + 1
                        elif Spwm[i] == TxSpwm[i]:
                                pass
                        elif Spwm[i] < TxSpwm[i]:
                                TxSpwm[i] = TxSpwm[i] - 1
                time.sleep(0.005)

def TelemetriKaydet():
        global durmasarti
        while durmasarti:
                time.sleep(0.1)
                with open("telemetri_verisi.txt", "w") as dosya:
                        dosya.write(", ".join(map(str, telemetri)))
                        

def sicaklik_oku_vcgencmd():
        global durmasarti, telemetri
        while durmasarti:
                try:
                        sonuc = subprocess.check_output(["vcgencmd", "measure_temp"]).decode("utf-8")
                        telemetri[9] = float(sonuc.split("=")[1].split("'")[0])
                        time.sleep(0.01)
                except FileNotFoundError:
                        print("vcgencmd bulunamadi")
                
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
        global TxSpwm
        global Spwm
        MotorDeger = [125,125,125,125,125,125,125,125]
        while True:
                sayilar = [int(veri) for veri in open("TestMotorSayisalRT.txt", "r").read().split(',') if veri.isdigit()]
                Spwm = sayilar[-4:]
                MotorDeger = sayilar[:8]
                time.sleep(0.01)

i2c_lock = threading.Lock()

def write_register(register, value):
    with i2c_lock:
        bus.write_byte_data(MPU9250_ADDRESS, register, value)

def read_registers(register, length):
    with i2c_lock:
        return bus.read_i2c_block_data(MPU9250_ADDRESS, register, length)

def initialize_mpu9250():
    write_register(PWR_MGMT_1, 0x00)
    time.sleep(0.1)
    write_register(ACCEL_CONFIG, 0x00)
    write_register(GYRO_CONFIG, 0x00)

def combine_bytes(high, low):
    value = (high << 8) | low
    if value > 32767:
        value -= 65536
    return value

def read_accel_gyro_temp():
    data = read_registers(ACCEL_XOUT_H, 14)
    accel_x = combine_bytes(data[0], data[1])
    accel_y = combine_bytes(data[2], data[3])
    accel_z = combine_bytes(data[4], data[5])
    temp_raw = combine_bytes(data[6], data[7])
    gyro_x = combine_bytes(data[8], data[9])
    gyro_y = combine_bytes(data[10], data[11])
    gyro_z = combine_bytes(data[12], data[13])
    return accel_x, accel_y, accel_z, temp_raw, gyro_x, gyro_y, gyro_z

def convert_accel_raw_to_ms2(raw_value):
    return round(raw_value / 16384.0 * 9.81 ,3)

def convert_gyro_raw_to_dps(raw_value):
    return round(raw_value / 131.0 , 3)

def convert_temp_raw_to_celsius(raw_value):
    return round((raw_value / 333.87) + 21.0, 3)

def sensor_task():
        global dogrulamabyti, durmasarti, TxSpwm, Spwm, TxMotorverisi, MotorDeger, TxData, RxData, telemetri
        while True:
                try:
                        accel_x, accel_y, accel_z, temp_raw, gyro_x, gyro_y, gyro_z = read_accel_gyro_temp()
                        telemetri[0] = accel_x_ms2 = convert_accel_raw_to_ms2(accel_x)
                        telemetri[1] = accel_y_ms2 = convert_accel_raw_to_ms2(accel_y)
                        telemetri[2] = accel_z_ms2 = convert_accel_raw_to_ms2(accel_z)
                        telemetri[3] = gyro_x_dps = convert_gyro_raw_to_dps(gyro_x)
                        telemetri[4] = gyro_y_dps = convert_gyro_raw_to_dps(gyro_y)
                        telemetri[5] = gyro_z_dps = convert_gyro_raw_to_dps(gyro_z)
                        telemetri[11] = temperature_c = convert_temp_raw_to_celsius(temp_raw)
                        time.sleep(0.01)
                except:
                        pass

                TxData = [dogrulamabyti] + TxMotorverisi + [dogrulamabyti] + TxSpwm + [dogrulamabyti]
                try:
                        ser.write(TxData)
                except:
                        pass

                #os.system("clear")
                print(",".join(map(str, telemetri)) + "," + ",".join(map(str, TxData)))
                

                time.sleep(0.010)

if __name__ == '__main__':
        durmasarti = True
        os.chdir("Belirsiz_Adres")
        try:
                initialize_mpu9250()
        except:
                pass
        t1 = threading.Thread(target = MotorValueUpdate)
        t2 = threading.Thread(target = MotorDegerUpdate)
        t4 = threading.Thread(target = TelemetriKaydet)
        t5 = threading.Thread(target = sicaklik_oku_vcgencmd)
        #t7 = threading.Thread(target = UartRead)
        t8 = threading.Thread(target = sensor_task)

        t1.start()
        t2.start()
        t4.start()
        t5.start()
        #t7.start()
        t8.start()

        t1.join()
        t2.join()
        t4.join()
        t5.join()
        #t7.join()
        t8.join()
        print("durdu")
        