
#include <Servo.h>

Servo servo1;             //Arm beam 1
Servo servo2              //Arm beam 2
Servo servo3;             //Arm base


pos1 = 90;                //Sets initial position of the servos to 90 degrees 
pos2 = 90;
pos3 = 90;

void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);
  
  servo1.attach(5);                 //Attaches servos to pins
  servo2.attach(6);
  servo3.attach(7);

  }

void loop() {
  // put your main code here, to run repeatedly:
  
  pos1 = Serial.read();             //sweeps the servo to a certain angle based on input from C# code
  servo1.write(pos1);
  delay(15);

  pos2 = Serial.read();
  servo2.write(pos2);
  delay(15;
  
  pos3 = Serial.read();
  servo3.write(pos3);
  delay(15);

}
