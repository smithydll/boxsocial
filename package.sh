#!/bin/bash

rm -R Package
mkdir Package

cp Blog/bin/Debug/Blog.dll Package/
cp Blog/bin/Debug/Blog.mdb Package/
cp Blog/languages/Blog.en.resources Package/

cp GuestBook/bin/Debug/GuestBook.dll Package/
cp GuestBook/bin/Debug/GuestBook.mdb Package/
cp GuestBook/languages/GuestBook.en.resources Package/

cp Profile/bin/Debug/Profile.dll Package/
cp Profile/bin/Debug/Profile.mdb Package/
cp Profile/languages/Profile.en.resources Package/

cp Calendar/bin/Debug/Calendar.dll Package/
cp Calendar/bin/Debug/Calendar.mdb Package/
cp Calendar/languages/Calendar.en.resources Package/

cp Gallery/bin/Debug/Gallery.dll Package/
cp Gallery/bin/Debug/Gallery.mdb Package/
cp Gallery/languages/Gallery.en.resources Package/

cp Pages/bin/Debug/Pages.dll Package/
cp Pages/bin/Debug/Pages.mdb Package/
cp Pages/languages/Pages.en.resources Package/

cp Forum/bin/Debug/Forum.dll Package/
cp Forum/bin/Debug/Forum.mdb Package/
cp Forum/languages/Forum.en.resources Package/

cp News/bin/Debug/News.dll Package/
cp News/bin/Debug/News.mdb Package/
cp News/languages/News.en.resources Package/

cp Mail/bin/Debug/Mail.dll Package/
cp Mail/bin/Debug/Mail.mdb Package/
cp Mail/languages/Mail.en.resources Package/

cp BoxSocial.Forms/bin/Debug/BoxSocial.Forms.dll Package/
cp BoxSocial.Forms/bin/Debug/BosSocial.Forms.mdb Package/
cp BoxSocial.IO/bin/Debug/BoxSocial.IO.dll Package/
cp BoxSocial.IO/bin/Debug/BoxSocial.IO.mdb Package/

cp Groups/bin/Debug/Groups.dll Package/
cp Groups/bin/Debug/Groups.mdb Package/
cp Groups/languages/Groups.en.resources Package/

cp Networks/bin/Debug/Networks.dll Package/
cp Networks/bin/Debug/Networks.mdb Package/
cp Networks/languages/Networks.en.resources Package/

cp Musician/bin/Debug/Musician.dll Package/
cp Musician/bin/Debug/Musician.mdb Package/
cp Musician/languages/Musician.en.resources Package/

cp BoxSocial.Internals/bin/Debug/BoxSocial.Internals.dll Package/
cp BoxSocial.Internals/bin/Debug/BoxSocial.Internals.mdb Package/
cp BoxSocial/languages/Internals.en.resources Package/

cp BoxSocial.FrontEnd/bin/Debug/BoxSocial.FrontEnd.dll Package/
cp BoxSocial.FrontEnd/bin/Debug/BoxSocial.FrontEnd.mdb Package/

cp Dependencies/bin/MySql.Data.dll Package/
cp Dependencies/bin/AWSSDK.dll Package/

cp BoxSocial.Install/bin/Debug/BoxSocial.Install.exe Package/
cp BoxSocial.Install/bin/Debug/BoxSocial.Install.mdb Package/

cp license.txt Package/

cp INSTALL.txt Package/

cp --recursive GDK Package/

cp --recursive templates Package/

cp --recursive styles Package/

cp --recursive scripts Package/

rm BoxSocial.tar.gz
tar -cf BoxSocial.tar Package/*
gzip BoxSocial.tar
