echo \
"KERNEL==\"ttyACM[0-9]*\",MODE=\"0666\"
KERNEL==\"ttyUSB[0-9]*\",MODE=\"0666\" \
" > /etc/udev/rules.d/99-usb-serial.rules