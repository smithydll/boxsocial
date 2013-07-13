erase /F /Q "Package"

md "%1"Package\applications

copy "%1"Blog\bin\Debug\Blog.dll "%1"Package\
copy "%1"Blog\bin\Debug\Blog.dll "%1"Package\applications\
copy "%1"Blog\bin\Debug\Blog.pdb "%1"Package\
copy "%1"Blog\languages\Blog.en.resources "%1"Package\

copy "%1"GuestBook\bin\Debug\GuestBook.dll "%1"Package\
copy "%1"GuestBook\bin\Debug\GuestBook.dll "%1"Package\applications\
copy "%1"GuestBook\bin\Debug\GuestBook.pdb "%1"Package\
copy "%1"GuestBook\languages\GuestBook.en.resources "%1"Package\

copy "%1"Profile\bin\Debug\Profile.dll "%1"Package\
copy "%1"Profile\bin\Debug\Profile.dll "%1"Package\applications\
copy "%1"Profile\bin\Debug\Profile.pdb "%1"Package\
copy "%1"Profile\languages\Profile.en.resources "%1"Package\

copy "%1"Calendar\bin\Debug\Calendar.dll "%1"Package\
copy "%1"Calendar\bin\Debug\Calendar.dll "%1"Package\applications\
copy "%1"Calendar\bin\Debug\Calendar.pdb "%1"Package\
copy "%1"Calendar\languages\Calendar.en.resources "%1"Package\

copy "%1"Gallery\bin\Debug\Gallery.dll "%1"Package\
copy "%1"Gallery\bin\Debug\Gallery.dll "%1"Package\applications\
copy "%1"Gallery\bin\Debug\Gallery.pdb "%1"Package\
copy "%1"Gallery\languages\Gallery.en.resources "%1"Package\

copy "%1"Pages\bin\Debug\Pages.dll "%1"Package\
copy "%1"Pages\bin\Debug\Pages.dll "%1"Package\applications\
copy "%1"Pages\bin\Debug\Pages.pdb "%1"Package\
copy "%1"Pages\languages\Pages.en.resources "%1"Package\

copy "%1"Forum\bin\Debug\Forum.dll "%1"Package\
copy "%1"Forum\bin\Debug\Forum.dll "%1"Package\applications\
copy "%1"Forum\bin\Debug\Forum.pdb "%1"Package\
copy "%1"Forum\languages\Forum.en.resources "%1"Package\

copy "%1"News\bin\Debug\News.dll "%1"Package\
copy "%1"News\bin\Debug\News.dll "%1"Package\applications\
copy "%1"News\bin\Debug\News.pdb "%1"Package\
copy "%1"News\languages\News.en.resources "%1"Package\

copy "%1"Mail\bin\Debug\Mail.dll "%1"Package\
copy "%1"Mail\bin\Debug\Mail.dll "%1"Package\applications\
copy "%1"Mail\bin\Debug\Mail.pdb "%1"Package\
copy "%1"Mail\languages\Mail.en.resources "%1"Package\

copy "%1"EnterpriseResourcePlanning\bin\Debug\EnterpriseResourcePlanning.dll "%1"Package\
copy "%1"EnterpriseResourcePlanning\bin\Debug\EnterpriseResourcePlanning.dll "%1"Package\applications\
copy "%1"EnterpriseResourcePlanning\bin\Debug\EnterpriseResourcePlanning.pdb "%1"Package\
copy "%1"EnterpriseResourcePlanning\languages\EnterpriseResourcePlanning.en.resources "%1"Package\

copy "%1"BoxSocial.Forms\bin\Debug\BoxSocial.Forms.dll "%1"Package\
copy "%1"BoxSocial.Forms\bin\Debug\BoxSocial.Forms.pdb "%1"Package\
copy "%1"BoxSocial.KnowledgeBase\bin\Debug\BoxSocial.KnowledgeBase.dll "%1"Package\
copy "%1"BoxSocial.KnowledgeBase\bin\Debug\BoxSocial.KnowledgeBase.pdb "%1"Package\
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

md "%1"Package\GDK

copy "%1"BoxSocial\GDK "%1"Package\GDK

md "%1"Package\templates

copy "%1"BoxSocial\templates "%1"Package\templates

md "%1"Package\templates\emails

copy "%1"BoxSocial\templates\emails "%1"Package\templates\emails

md "%1"Package\templates\mobile

copy "%1"BoxSocial\templates\mobile "%1"Package\templates\mobile

md "%1"Package\templates\tablet

copy "%1"BoxSocial\templates\tablet "%1"Package\templates\tablet

md "%1"Package\styles

copy "%1"BoxSocial\styles "%1"Package\styles

md "%1"Package\styles\images

copy "%1"BoxSocial\styles\images "%1"Package\styles\images

md "%1"Package\scripts

copy "%1"BoxSocial\scripts "%1"Package\scripts

md "%1"Package\www

copy "%1"BoxSocial "%1"Package\www