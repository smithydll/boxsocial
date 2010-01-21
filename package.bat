erase /F /Q "Package"
copy "%1"Blog\bin\Release\Blog.dll "%1"Package\
copy "%1"Blog\bin\Release\Blog.pdb "%1"Package\
copy "%1"Blog\languages\Blog.en.resources "%1"Package\

copy "%1"GuestBook\bin\Release\GuestBook.dll "%1"Package\
copy "%1"GuestBook\bin\Release\GuestBook.pdb "%1"Package\
copy "%1"GuestBook\languages\GuestBook.en.resources "%1"Package\

copy "%1"Profile\bin\Release\Profile.dll "%1"Package\
copy "%1"Profile\bin\Release\Profile.pdb "%1"Package\
copy "%1"Profile\languages\Profile.en.resources "%1"Package\

copy "%1"Calendar\bin\Release\Calendar.dll "%1"Package\
copy "%1"Calendar\bin\Release\Calendar.pdb "%1"Package\
copy "%1"Calendar\languages\Calendar.en.resources "%1"Package\

copy "%1"Gallery\bin\Release\Gallery.dll "%1"Package\
copy "%1"Gallery\bin\Release\Gallery.pdb "%1"Package\
copy "%1"Gallery\languages\Gallery.en.resources "%1"Package\

copy "%1"Pages\bin\Release\Pages.dll "%1"Package\
copy "%1"Pages\bin\Release\Pages.pdb "%1"Package\
copy "%1"Pages\languages\Pages.en.resources "%1"Package\

copy "%1"Forum\bin\Release\Forum.dll "%1"Package\
copy "%1"Forum\bin\Release\Forum.pdb "%1"Package\
copy "%1"Forum\languages\Forum.en.resources "%1"Package\

copy "%1"News\bin\Release\News.dll "%1"Package\
copy "%1"News\bin\Release\News.pdb "%1"Package\
copy "%1"News\languages\News.en.resources "%1"Package\

copy "%1"Mail\bin\Release\Mail.dll "%1"Package\
copy "%1"Mail\bin\Release\Mail.pdb "%1"Package\
copy "%1"Mail\languages\Mail.en.resources "%1"Package\

copy "%1"BoxSocial.Forms\bin\Release\BoxSocial.Forms.dll "%1"Package\
copy "%1"BoxSocial.Forms\bin\Release\BoxSocial.Forms.pdb "%1"Package\
copy "%1"BoxSocial.IO\bin\Release\BoxSocial.IO.dll "%1"Package\
copy "%1"BoxSocial.IO\bin\Release\BoxSocial.IO.pdb "%1"Package\
copy "%1"Groups\bin\Release\Groups.dll "%1"Package\
copy "%1"Groups\bin\Release\Groups.pdb "%1"Package\
copy "%1"Networks\bin\Release\Networks.dll "%1"Package\
copy "%1"Networks\bin\Release\Networks.pdb "%1"Package\
copy "%1"Musician\bin\Release\Musician.dll "%1"Package\
copy "%1"Musician\bin\Release\Musician.pdb "%1"Package\
copy "%1"BoxSocial.Internals\bin\Release\BoxSocial.Internals.dll "%1"Package\
copy "%1"BoxSocial.Internals\bin\Release\BoxSocial.Internals.pdb "%1"Package\
copy "%1"BoxSocial.Internals\languages\Internals.en.resources "%1"Package\

copy "%1"BoxSocial.FrontEnd\bin\Release\BoxSocial.FrontEnd.dll "%1"Package\
copy "%1"BoxSocial.FrontEnd\bin\Release\BoxSocial.FrontEnd.pdb "%1"Package\

copy "%1"BoxSocial\bin\MySql.Data.dll "%1"Package\

copy "%1"BoxSocial.Install\bin\Release\BoxSocial.Install.exe "%1"Package\
copy "%1"BoxSocial.Install\bin\Release\BoxSocial.Install.pdb "%1"Package\
