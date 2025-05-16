create schema if not exists "user-hub" AUTHORIZATION admin;

create table "user-hub"."users"
(
   id serial primary key,
   login varchar(20),
   password varchar(20)
);