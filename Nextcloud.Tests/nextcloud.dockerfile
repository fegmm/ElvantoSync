FROM nextcloud:latest
ENV MYSQL_DATABASE=nextcloud
ENV MYSQL_USER=root
ENV MYSQL_PASSWORD=dbPassword123!
ENV MYSQL_HOST=localhost:3306
ENV NEXTCLOUD_ADMIN_USER=admin
ENV NEXTCLOUD_ADMIN_PASSWORD=StrongPassword123!


# install Nextcloud via call to entrypoint.sh
RUN /entrypoint.sh apache2-foreground & pid=$! && sleep 60 && kill $pid

USER www-data

# install Nextcloud Apps
RUN cd /var/www/html && \
    php occ app:install deck && \
    php occ app:install spreed && \
    php occ app:install contacts && \
    php occ app:install collectives && \
    php occ app:install groupfolders

USER root
