del /F tm-Wants.txt
copy /V /Y tm-Options.txt /A + ExchangeLists-Wants.txt /A tm-Wants.txt /A
java -jar tm.jar < tm-Wants.txt > tm-Results.txt
