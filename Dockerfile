FROM node:20 AS build
WORKDIR /app

COPY project-claient/package*.json ./
RUN npm install

COPY project-claient/ ./
RUN npm run build -- --configuration production

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/project-claient/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
