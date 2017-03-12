var InputBox = React.createClass(
{
    getInitialState: function()
    {
        return {value: ''};
    },
    handleSubmit: function(e)
    {
        e.preventDefault();
        this.props.onInputSubmit({line:this.state.value});
    },
    handleValueChange: function(e)
    {
        this.setState({value: e.target.value});
    },
    render: function()
    {
        return (
            <form className="inputform" onSubmit={this.handleSubmit}>
                <input className="line-id-field" type="text" placeholder="...type the line number here" onChange={this.handleValueChange} />
                <input className="submit-button" type="submit" value="GO!"/>
            </form>
               );
    }
});

var OutputBox = React.createClass(
{
    render: function()
    {
        var scheduleItems = this.props.data.map(function(e)
        {
            var string='';
            e.scheduleString.forEach(function(item)
            {
                string+= item + ' | ';
            });
            string = string.slice(0,-3);
            return (
                <OutputItem key={e.id} name={e.stationName}>             
                    {string}          
                </OutputItem>
                   );
        });
        return (
        <table className="output-table">
            <tbody>
            {scheduleItems}
            </tbody>
        </table>
        );
    }
});

var OutputItem = React.createClass(
{
    render: function()
    {
        return (
            <tr className="infoline">
                <td className="first-column">{this.props.name}</td>
                <td className="second-column">{this.props.children}</td>
            </tr>
               );
    }
});

var StateInfo = React.createClass(
{
    render: function()
    {
        return(
            <p className="state-info">{this.props.info}</p>
              );
    }
});

var InfoBox = React.createClass(
{
    getInitialState: function()
    {
        return {data: [], info:''};
    },
    handleInputSubmit: function(e)
    {
        this.setState({data:[]});
        this.setState({info:''});
        if(e.line !== '')
        {
            var requestURL = this.props.url + '/' + e.line;
            var xhr = new XMLHttpRequest();
            xhr.open('get',requestURL,true);
            xhr.onloadstart = function()
            {
                var info = 'Loading... Please wait';
                this.setState({info:info});
            }.bind(this);
            xhr.onreadystatechange = function() 
            {
                if(xhr.readyState == 4)
                {
                    if(xhr.status == 200)
                    {
                        if(xhr.responseText != '"[]"')
                        {
                            var info = e.line.toUpperCase() + ' SCHEDULE';
                            this.setState({info:info});
                            var data = JSON.parse(xhr.responseText);
                            this.setState({ data: data });
                        }
                        else
                        {
                            this.setState({info:'The line you are looking for does not exist. Try specifying a different one'});
                        }
                    }
                }
                else
                {
                    var info = 'Error. Could not retrieve data';
                    this.setState({info:info});
                }
            }.bind(this);
            xhr.send();
        }
    },
    render: function()
    {
        return (
        <div className="infobox">
            <p className="startuptext"><b>ENTER THE LINE NUMBER AND PRESS THE BUTTON TO SEE THE SCHEDULE</b></p>
            <InputBox onInputSubmit={this.handleInputSubmit}/>
            <StateInfo info={this.state.info} />
            <OutputBox data={this.state.data} />
        </div>
               );
    }
});

ReactDOM.render(
    <InfoBox url="/schedule" />,
    document.getElementById('content')
);