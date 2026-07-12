# שלב 1: בניית קוד ה-Angular באמצעות Node.js
FROM node:20 AS build
WORKDIR /app

# העתקת קבצי ההגדרות והתקנת התלויות
COPY project-claient/package*.json ./
RUN npm install

# העתקת כל שאר קוד הלקוח ובניית הפרויקט לפרודקשן
COPY project-claient/ ./
RUN npm run build -- --configuration production

# שלב 2: הגשת הקבצים הסטטיים באמצעות שרת Nginx קליל
FROM nginx:alpine
# העתקת הקוד הבנוי מתוך שלב הבנייה אל תיקיית ההגשה של Nginx
# שימי לב: לעיתים התיקייה בתוך dist נקראת על שם הפרויקט שלך (למשל dist/project-claient/browser)
COPY --from=build /app/dist/project-claient /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]