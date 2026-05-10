# Лабораторна робота №1: Web-сервіс з автоматизацією



## Варіант індивідуального завдання

**Номер у групі:** N = 9

Обчислення:
- V2 = (N % 2) + 1 = (9 % 2) + 1 = 1 + 1 = **2**
- V3 = (N % 3) + 1 = (9 % 3) + 1 = 0 + 1 = **1**
- V5 = (N % 5) + 1 = (9 % 5) + 1 = 4 + 1 = **5**

**Результат:**
- Тематика застосунку (V3=1): **Notes Service** – сервіс для зберігання текстових нотаток.
- Спосіб конфігурації (V2=2): **Конфігураційний файл** (`/etc/mywebapp/config.json`).
- СУБД (V2=2): **PostgreSQL**.
- Порт застосунку (V5=5): **5000**.

---



## Призначення

RESTful сервіс для керування нотатками. Дозволяє створити нотатку, переглянути список усіх нотаток та отримати детальну інформацію про конкретну нотатку. Дані зберігаються в базі даних PostgreSQL.

## Технології

- **Мова програмування:** C# 12 / .NET 10
- **Фреймворк:** ASP.NET Core
- **ORM:** Entity Framework Core
- **База даних:** PostgreSQL
- **Reverse Proxy:** Nginx
- **Керування процесом:** systemd (з socket activation)

## Документація API

Всі ендпоінти (крім кореневого та health) підтримують content negotiation:

    Якщо заголовок Accept: text/html – повертається проста HTML-сторінка (таблиця або деталі).

    Інакше (або Accept: application/json) – повертається JSON.


|  метод   |   URL  |  Опис   |
| --- | --- | --- |
| GET | `/` | (тільки `text/html`) Список усіх ендпоінтів бізнес-логіки |
| GET | `/health/alive` | Перевірка живості сервісу (завжди 200 OK) |
| GET | `/health/ready` | Перевірка готовності (200 OK якщо БД доступна) |
| GET | `/notes` | Отримати всі нотатки (id, title) |
| POST | `/notes` | Створити нову нотатку |
| GET | `/notes/{id}` | Отримати повну інформацію про нотатку (id, title, created_at, content) |


### Приклади запитів (curl)

```bash
# Створення нотатки (JSON)
curl -X POST http://localhost:5000/notes -H "Content-Type: application/json" -d '{"title":"Привіт","content":"Світ"}'

# Отримати всі нотатки (HTML)
curl -H "Accept: text/html" http://localhost:5000/notes

# Отримати всі нотатки (JSON)
curl http://localhost:5000/notes

# Отримати нотатку з id=1 (JSON)
curl http://localhost:5000/notes/1

# Перевірка здоров'я
curl http://localhost:5000/health/alive
curl http://localhost:5000/health/ready
```

## Необхідні ресурси

Для того щоб розгорнути та скористатись даною лабою вам знадобиться наступне:
 - образ .iso [**Ubuntu Server**](https://ubuntu.com/download/server) ,бажано версії 24.04.4
 - Програма, яка здатна розгортати віртуальні машини (Virtualbox, VMware Workstation, etc...)
 - Стабільний доступ до інтернету

### Вимоги до комп'ютера

- **CPU:** 1 ядро (мінімум)
    
- **RAM:** 1 GB (рекомендовано 2 GB)
    
- **Диск:** 10 GB вільного місця
    
- **Мережа:** Доступ до інтернету (для встановлення пакетів)

## Розгортання

>[!WARNING]
> Увага! Скрипт setup.sh **ЗАБЛОКУЄ КОРИСТУВАЧА ЯКИЙ ЙОГО ЗАПУСТИВ**.
> Це є частиною спланованого функціоналу, тому **ОБОВ'ЯЗКОВО** запускайте **ЛИШЕ НА ВІРТУАЛЬНІ МАШИНІ** або на хості якого не жалко

 1. Створіть віртульну машину на основі раніше скачаного .iso файлу **Ubuntu Server**
 2. Пройдіть процес налаштування стандартного користувача
 3. Після того як все настроїться та ви здатні ввійти під 
 іменем свого нового користувача - клоніруйте даний репозиторій командою
  `git clone https://github.com/MaksymKravchukRB/comp-systems-lab1.git`
4. Увійдіть у ново-створену папку командою `cd comp-systems-lab1.git`
5. Запустіть скрипт розгортання програми під адміністраторськими правами командою
`sudo ./setup.sh`

### Що зробить скрипт setup.sh

Для наглядності:

1. Встановить пакети (.NET SDK, PostgreSQL, Nginx).

2. Створить користувачів `app`, `student`, `teacher`, `operator`.
    
3. Налаштує sudo для `operator` (лише управління сервісом та перезавантаження nginx).
    
4. Створить базу даних та користувача PostgreSQL.
    
5. Згенерує конфігураційний файл `/etc/mywebapp/config.json`.
    
6. Скомпілює та опублікує застосунок в `/opt/mywebapp`.
    
7. Встановить systemd сервіс `mywebapp.service` (з міграцією БД перед запуском).
    
8. Налаштує Nginx як reverse proxy на порт 80.
    
9. Створить файл `/home/student/gradebook` з числом `9`.
    
10. Заблокує початкового користувача (того, хто запустив `sudo`).

Якщо скрипт `setup.sh` спрацював без помилок, то ви можете безпечно виходити з стандартного користувача та заходити в систему під одним з нових логінів та паролів.


## Облікові дані після розгортання
 Використовуйте створених користувачів:

- `student` – пароль `student123` (змініть після першого входу)
    
- `teacher` – пароль `12345678` (змінити при першому вході)
    
- `operator` – пароль `12345678` (змінити при першому вході)



### Перевірка коректності розгортання

1. **Переконайтеся, що сервіс запущений:**
    
    ```bash
    sudo systemctl status mywebapp
    ```
    
    Має бути `active (running)`.
    
2. **Перевірте доступність через reverse proxy (порт 80):**
    
	```bash
    curl http://localhost/health/alive
    curl http://localhost/health/ready
	```
    
    Очікується `OK` для обох.
    
3. **Створіть нотатку через Nginx:**
    
    ```bash
    curl -X POST http://localhost/notes \
         -H "Content-Type: application/json" \
         -d '{"title":"Test","content":"Hello"}'
    ```
    
4. **Отримайте список нотаток у JSON:**
    
    ```bash
    curl http://localhost/notes
    ```
    
5. **Отримайте HTML-версію списку:**
    

    ```bash
    curl -H "Accept: text/html" http://localhost/notes
    
    ```
    Має повернути HTML таблицю.
    
6. **Перевірте кореневий ендпоінт:**
    

    ```bash
    curl -H "Accept: text/html" http://localhost/
    ```
    
7. **Перевірте права користувача `operator`:**
    
    - Увійдіть як `operator`:
		- Виконайте дозволені команди:
        ```bash
        sudo systemctl restart mywebapp
        sudo systemctl status mywebapp
        sudo nginx -s reload
        ```
    - Спробуйте заборонену команду (наприклад, `sudo apt update`) – має відмовити.
        
8. **Перевірте файл gradebook:**
    
    ```bash
    cat /home/student/gradebook
    ```
    
    Має вивести `9`.

### Перевірка в ручному режимі (без автоматизації)

Якщо потрібно протестувати окремі компоненти, скористайтеся інструкціями з розділу [[#Приклади запитів (curl)]]



## Налаштування середовища для розробки
1. **Встановіть .NET 10 SDK**
```bash
sudo apt install dotnet-sdk-10.0
```
2. **Встановіть PostgreSQL**
```bash
sudo apt install postgresql
```
3. **Створіть базу даних та користувача**
```sql
sudo -u postgres psql
CREATE USER mywebapp WITH PASSWORD 'mysecretpassword';
CREATE DATABASE notesdb OWNER mywebapp;
\q
```
4. **Створіть конфігураційний файл** `/etc/mywebapp/config.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=notesdb;Username=mywebapp;Password=mysecretpassword"
  },
  "Port": 5000
}
```
5. **Застосуйте міграції**
```bash
dotnet run -- --migrate
```
6. **Запуск у режимі розробки**
```bash
dotnet run
```
Сервіс буде доступний за адресою `http://127.0.0.1:5000`.


## Розгортання за допомогою Docker Compose

### Передумови
- На цільовій машині мають бути встановлені **Docker** та **Docker Compose**.

### Інструкція з запуску
1. Клонуйте репозиторій проекту.
2. Зберіть та запустіть всі сервіси:
   ```bash
   docker-compose up -d
   ```
3. Сервіс нотаток буде доступний за адресою http://localhost (порт 80).
4. Зупиніть сервіси:
   ```bash
   docker-compose down
   ```

### Збереження даних (Volumes)

Дані PostgreSQL зберігаються у тому Docker-Volume postgres_data, що гарантує їх збереження після перезапуску контейнерів або системи.


### Конфігурація

Web-застосунок отримує рядок підключення до бази даних через змінну середовища DB_CONNECTION_STRING (вказана у docker-compose.yml).

Порт застосунку можна змінити через змінну PORT.


### Мережа

Всі сервіси комунікують у виділеній мережі app-network (bridge-драйвер).


### Міграції бази даних

Скрипт entrypoint.sh виконує dotnet mywebapp.dll --migrate перед запуском веб-сервера, що автоматично оновлює схему бази даних.
