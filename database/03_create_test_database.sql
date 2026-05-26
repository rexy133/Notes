-- Создание отдельной базы данных для автоматизированных тестов.

CREATE DATABASE notes_test_db
    WITH
    ENCODING = 'UTF8'
    TEMPLATE = template0;
