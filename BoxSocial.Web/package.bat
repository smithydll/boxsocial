erase /F /Q "Package"

md "%1Package\applications"

copy "%1Blog\bin\%2\Blog.dll" "%1Package\"
copy "%1Blog\bin\%2\Blog.dll" "%1Package\applications\"
copy "%1Blog\bin\%2\Blog.pdb" "%1Package\"
copy "%1Blog\languages\Blog.en.resources" "%1Package\"

copy "%1GuestBook\bin\%2\GuestBook.dll" "%1Package\"
copy "%1GuestBook\bin\%2\GuestBook.dll" "%1Package\applications\"
copy "%1GuestBook\bin\%2\GuestBook.pdb" "%1Package\"
copy "%1GuestBook\languages\GuestBook.en.resources" "%1Package\"

copy "%1Profile\bin\%2\Profile.dll" "%1Package\"
copy "%1Profile\bin\%2\Profile.dll" "%1Package\applications\"
copy "%1Profile\bin\%2\Profile.pdb" "%1Package\"
copy "%1Profile\languages\Profile.en.resources" "%1Package\"

copy "%1Calendar\bin\%2\Calendar.dll" "%1Package\"
copy "%1Calendar\bin\%2\Calendar.dll" "%1Package\applications\"
copy "%1Calendar\bin\%2\Calendar.pdb" "%1Package\"
copy "%1Calendar\languages\Calendar.en.resources" "%1Package\"

copy "%1Gallery\bin\%2\Gallery.dll" "%1Package\"
copy "%1Gallery\bin\%2\Gallery.dll" "%1Package\applications\"
copy "%1Gallery\bin\%2\Gallery.pdb" "%1Package\"
copy "%1Gallery\languages\Gallery.en.resources" "%1Package\"

copy "%1Pages\bin\%2\Pages.dll" "%1Package\"
copy "%1Pages\bin\%2\Pages.dll" "%1Package\applications\"
copy "%1Pages\bin\%2\Pages.pdb" "%1Package\"
copy "%1Pages\languages\Pages.en.resources" "%1Package\"

copy "%1Forum\bin\%2\Forum.dll" "%1Package\"
copy "%1Forum\bin\%2\Forum.dll" "%1Package\applications\"
copy "%1Forum\bin\%2\Forum.pdb" "%1Package\"
copy "%1Forum\languages\Forum.en.resources" "%1Package\"

copy "%1News\bin\%2\News.dll" "%1Package\"
copy "%1News\bin\%2\News.dll" "%1Package\applications\"
copy "%1News\bin\%2\News.pdb" "%1Package\"
copy "%1News\languages\News.en.resources" "%1Package\"

copy "%1Mail\bin\%2\Mail.dll" "%1Package\"
copy "%1Mail\bin\%2\Mail.dll" "%1Package\applications\"
copy "%1Mail\bin\%2\Mail.pdb" "%1Package\"
copy "%1Mail\languages\Mail.en.resources "%1Package\"

copy "%1EnterpriseResourcePlanning\bin\%2\EnterpriseResourcePlanning.dll" "%1Package\"
copy "%1EnterpriseResourcePlanning\bin\%2\EnterpriseResourcePlanning.dll" "%1Package\applications\"
copy "%1EnterpriseResourcePlanning\bin\%2\EnterpriseResourcePlanning.pdb" "%1Package\"
copy "%1EnterpriseResourcePlanning\languages\EnterpriseResourcePlanning.en.resources" "%1Package\"

copy "%1BoxSocial.Forms\bin\%2\BoxSocial.Forms.dll" "%1Package\"
copy "%1BoxSocial.Forms\bin\%2\BoxSocial.Forms.pdb" "%1Package\"
copy "%1BoxSocial.KnowledgeBase\bin\%2\BoxSocial.KnowledgeBase.dll" "%1Package\"
copy "%1BoxSocial.KnowledgeBase\bin\%2\BoxSocial.KnowledgeBase.pdb" "%1Package\"
copy "%1BoxSocial.IO\bin\%2\BoxSocial.IO.dll" "%1Package\"
copy "%1BoxSocial.IO\bin\%2\BoxSocial.IO.pdb" "%1Package\"
copy "%1Groups\bin\%2\Groups.dll" "%1Package\"
copy "%1Groups\bin\%2\Groups.pdb" "%1Package\"
copy "%1Groups\languages\Groups.en.resources" "%1Package\"
copy "%1Networks\bin\%2\Networks.dll" "%1Package\"
copy "%1Networks\bin\%2\Networks.pdb" "%1Package\"
copy "%1Musician\bin\%2\Musician.dll" "%1Package\"
copy "%1Musician\bin\%2\Musician.pdb" "%1Package\"
copy "%1Musician\languages\Musician.en.resources" "%1Package\"
copy "%1BoxSocial.Internals\bin\%2\BoxSocial.Internals.dll" "%1Package\"
copy "%1BoxSocial.Internals\bin\%2\BoxSocial.Internals.pdb" "%1Package\"
copy "%1BoxSocial.Internals\languages\Internals.en.resources" "%1Package\"

copy "%1BoxSocial.FrontEnd\bin\%2\BoxSocial.FrontEnd.dll" "%1Package\"
copy "%1BoxSocial.FrontEnd\bin\%2\BoxSocial.FrontEnd.pdb" "%1Package\"

copy "%1BoxSocial\bin\MySql.Data.dll" "%1Package\"

copy "%1BoxSocial.Install\bin\%2\BoxSocial.Install.exe" "%1Package\"
copy "%1BoxSocial.Install\bin\%2\BoxSocial.Install.pdb" "%1Package\"

md "%1Package\GDK"

copy "%1GDK\*.svg" "%1Package\GDK"

md "%1Package\templates"

copy "%1BoxSocial\templates" "%1Package\templates"

md "%1Package\templates\emails"

copy "%1BoxSocial\templates\emails" "%1Package\templates\emails"

md "%1Package\templates\mobile"

copy "%1BoxSocial\templates\mobile" "%1Package\templates\mobile"

md "%1Package\templates\tablet"

copy "%1BoxSocial\templates\tablet" "%1Package\templates\tablet"

md "%1Package\styles"

copy "%1BoxSocial\styles" "%1Package\styles"

md "%1Package\styles\images"

copy "%1BoxSocial\styles\images" "%1Package\styles\images"

md "%1Package\scripts"

copy "%1BoxSocial\scripts" "%1Package\scripts"

md "%1Package\www"

copy "%1BoxSocial" "%1Package\www"

copy "%1Dependencies\bin" "%1Package"