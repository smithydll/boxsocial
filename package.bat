erase /F /Q "Package"
copy "%1"Blog\bin\Debug\Blog.dll "%1"Package\
copy "%1"Blog\bin\Debug\Blog.pdb "%1"Package\
copy "%1"Blog\languages\Blog.en.resources "%1"Package\

copy "%1"GuestBook\bin\Debug\GuestBook.dll "%1"Package\
copy "%1"GuestBook\bin\Debug\GuestBook.pdb "%1"Package\
copy "%1"GuestBook\languages\GuestBook.en.resources "%1"Package\

copy "%1"Profile\bin\Debug\Profile.dll "%1"Package\
copy "%1"Profile\bin\Debug\Profile.pdb "%1"Package\
copy "%1"Profile\languages\Profile.en.resources "%1"Package\

copy "%1"Calendar\bin\Debug\Calendar.dll "%1"Package\
copy "%1"Calendar\bin\Debug\Calendar.pdb "%1"Package\
copy "%1"Calendar\languages\Calendar.en.resources "%1"Package\

copy "%1"Gallery\bin\Debug\Gallery.dll "%1"Package\
copy "%1"Gallery\bin\Debug\Gallery.pdb "%1"Package\
copy "%1"Gallery\languages\Gallery.en.resources "%1"Package\

copy "%1"Pages\bin\Debug\Pages.dll "%1"Package\
copy "%1"Pages\bin\Debug\Pages.pdb "%1"Package\
copy "%1"Pages\languages\Pages.en.resources "%1"Package\

copy "%1"Forum\bin\Debug\Forum.dll "%1"Package\
copy "%1"Forum\bin\Debug\Forum.pdb "%1"Package\
copy "%1"Forum\languages\Forum.en.resources "%1"Package\

copy "%1"News\bin\Debug\News.dll "%1"Package\
copy "%1"News\bin\Debug\News.pdb "%1"Package\
copy "%1"News\languages\News.en.resources "%1"Package\

copy "%1"Mail\bin\Debug\Mail.dll "%1"Package\
copy "%1"Mail\bin\Debug\Mail.pdb "%1"Package\
copy "%1"Mail\languages\Mail.en.resources "%1"Package\

copy "%1"BoxSocial.Forms\bin\Debug\BoxSocial.Forms.dll "%1"Package\
copy "%1"BoxSocial.Forms\bin\Debug\BoxSocial.Forms.pdb "%1"Package\
copy "%1"BoxSocial.IO\bin\Debug\BoxSocial.IO.dll "%1"Package\
copy "%1"BoxSocial.IO\bin\Debug\BoxSocial.IO.pdb "%1"Package\
copy "%1"Groups\bin\Debug\Groups.dll "%1"Package\
copy "%1"Groups\bin\Debug\Groups.pdb "%1"Package\
copy "%1"Groups\languages\Groups.en.resources "%1"Package\
copy "%1"Networks\bin\Debug\Networks.dll "%1"Package\
copy "%1"Networks\bin\Debug\Networks.pdb "%1"Package\
copy "%1"Musician\bin\Debug\Musician.dll "%1"Package\
copy "%1"Musician\bin\Debug\Musician.pdb "%1"Package\
copy "%1"Musician\languages\Musician.en.resources "%1"Package\
copy "%1"BoxSocial.Internals\bin\Debug\BoxSocial.Internals.dll "%1"Package\
copy "%1"BoxSocial.Internals\bin\Debug\BoxSocial.Internals.pdb "%1"Package\
copy "%1"BoxSocial.Internals\languages\Internals.en.resources "%1"Package\

copy "%1"BoxSocial.FrontEnd\bin\Debug\BoxSocial.FrontEnd.dll "%1"Package\
copy "%1"BoxSocial.FrontEnd\bin\Debug\BoxSocial.FrontEnd.pdb "%1"Package\

copy "%1"BoxSocial\bin\MySql.Data.dll "%1"Package\

copy "%1"BoxSocial.Install\bin\Debug\BoxSocial.Install.exe "%1"Package\
copy "%1"BoxSocial.Install\bin\Debug\BoxSocial.Install.pdb "%1"Package\
